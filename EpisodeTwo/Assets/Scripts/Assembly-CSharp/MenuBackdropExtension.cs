using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Episode Two's original artwork ends in machinery. Close those edges into
// the dark surround rather than duplicating mechanical parts or stretching them.
public sealed class MenuBackdropExtension : MonoBehaviour
{
    public Camera Camera;
    Renderer source;
    Material material;
    Material[] originalMaterials;
    Material[] backdropMaterials;
    readonly List<GameObject> objects = new List<GameObject>();
    readonly List<Mesh> meshes = new List<Mesh>();
    float lastSize = -1f, lastAspect = -1f;
    Vector3 nativeScale, nativePosition;
    bool hasNativeFrame;
    float fadeWidth;
    // Authored MainMenu background bounds; refreshed from the live main menu.
    // Other scenes use differently scaled copies of this same sprite.
    static float menuArtworkHeight = 604.8f;
    static Vector2 menuArtworkOffset = new Vector2(-3.98f, -16.78f);

    public static bool UsesMenuArtwork(Renderer backdrop)
    {
        var sprite = backdrop != null ? backdrop.GetComponent<tk2dBaseSprite>() : null;
        return sprite != null && sprite.collection != null && sprite.GetCurrentSpriteDef().name == "UI_Background";
    }

    public static bool TryApply(Camera camera, Renderer backdrop)
    {
        if (camera == null || !UsesMenuArtwork(backdrop) || (camera.cullingMask & (1 << backdrop.gameObject.layer)) == 0) return false;
        var extension = backdrop.GetComponent<MenuBackdropExtension>();
        if (extension == null) extension = backdrop.gameObject.AddComponent<MenuBackdropExtension>();
        extension.Camera = camera;
        extension.source = backdrop;
        return true;
    }

    IEnumerator Start()
    {
        yield return null;
        if (Camera == null) yield break;
        if (source == null) foreach (MainMenuScreen menu in Object.FindObjectsByType<MainMenuScreen>(FindObjectsInactive.Include))
        {
            if (menu.gameObject.scene != gameObject.scene) continue;
            Transform backdrop = menu.transform.Find("Background");
            if (backdrop != null) source = backdrop.GetComponent<Renderer>();
        }
        Shader shader = Resources.Load<Shader>("MenuEdgeExtension");
        if (source == null || shader == null) yield break;
        // The surround fades artwork; labels and controls must render above it.
        material = new Material(shader) { name = "Dark menu surround", renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 1 };
        originalMaterials = source.sharedMaterials;
        backdropMaterials = new Material[originalMaterials.Length];
        for (int i=0;i<backdropMaterials.Length;i++)
        {
            backdropMaterials[i]=new Material(shader);
            backdropMaterials[i].CopyPropertiesFromMaterial(originalMaterials[i]);
            backdropMaterials[i].SetFloat("_Backdrop",1f);
            backdropMaterials[i].renderQueue=originalMaterials[i].renderQueue;
        }
        source.sharedMaterials=backdropMaterials;
        Rebuild();
    }

    void LateUpdate()
    {
        if (source != null && material != null && Camera != null &&
            (!Mathf.Approximately(lastSize,Camera.orthographicSize) || !Mathf.Approximately(lastAspect,Camera.aspect))) Rebuild();
    }

    void Clear()
    {
        foreach (GameObject go in objects) if (go != null) Destroy(go);
        foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
        objects.Clear(); meshes.Clear();
    }

    void Rebuild()
    {
        if (source == null || Camera == null || material == null) return;
        FitArtwork();
        if (Mathf.Abs(Vector3.Dot(source.bounds.center-Camera.transform.position,Camera.transform.up)) >
            source.bounds.extents.y+Camera.orthographicSize) return;
        Clear();
        lastSize = Camera.orthographicSize; lastAspect = Camera.aspect;
        float center=Vector3.Dot(source.bounds.center,Camera.transform.up);
        float viewCenter=Vector3.Dot(Camera.transform.position,Camera.transform.up);
        float upper=center+source.bounds.extents.y, lower=center-source.bounds.extents.y;
        foreach (Material backdrop in backdropMaterials)
        {
            backdrop.SetVector("_Up",Camera.transform.up);
            backdrop.SetVector("_Edges",new Vector4(
                viewCenter-Camera.orthographicSize < lower ? lower : -1000000f,
                viewCenter+Camera.orthographicSize > upper ? upper : 1000000f,fadeWidth,0f));
        }
        Build(true); Build(false);
    }

    void FitArtwork()
    {
        Transform backdrop = source.transform;
        if (!hasNativeFrame)
        {
            nativeScale = backdrop.localScale;
            nativePosition = backdrop.localPosition;
            hasNativeFrame = true;
        }
        backdrop.localScale = nativeScale;
        backdrop.localPosition = nativePosition;
        float frameScale = Camera.orthographicSize / LegacyScreenFit.MainMenuCameraSize(Camera);
        fadeWidth = 40f * frameScale;
        if (gameObject.scene.name == "MainMenu")
        {
            menuArtworkHeight = source.bounds.size.y;
            Vector3 offset = source.bounds.center - Camera.transform.position;
            menuArtworkOffset = new Vector2(Vector3.Dot(offset, Camera.transform.right), Vector3.Dot(offset, Camera.transform.up));
            return;
        }
        float scale = menuArtworkHeight * frameScale / source.bounds.size.y;
        backdrop.localScale = Vector3.Scale(nativeScale, new Vector3(scale, scale, 1f));
        Vector3 target = Camera.transform.position + frameScale * (Camera.transform.right * menuArtworkOffset.x + Camera.transform.up * menuArtworkOffset.y);
        Vector3 delta = target - source.bounds.center;
        backdrop.position += Camera.transform.right * Vector3.Dot(delta, Camera.transform.right) + Camera.transform.up * Vector3.Dot(delta, Camera.transform.up);
    }

    void Build(bool top)
    {
        Vector3 up = Camera.transform.up, right = Camera.transform.right;
        Vector3 center = source.bounds.center - Camera.transform.forward*.1f;
        float sign = top ? 1f : -1f;
        float edge = source.bounds.extents.y;
        float visible = sign*Vector3.Dot(Camera.transform.position-source.bounds.center,up)+Camera.orthographicSize;
        if (visible <= edge) return;
        float[] heights = {edge-fadeWidth,edge,visible+.25f};
        var positions = new List<Vector3>(); var colors = new List<Color>();
        var triangles = new List<int>();
        for (int row=0;row<3;row++)
        {
            positions.Add(center+sign*heights[row]*up-source.bounds.extents.x*right);
            positions.Add(center+sign*heights[row]*up+source.bounds.extents.x*right);
            colors.Add(new Color(0f,0f,0f,row == 0 ? 0f : 1f));
            colors.Add(new Color(0f,0f,0f,row == 0 ? 0f : 1f));
            if (row == 0) continue;
            int previous=(row-1)*2, current=row*2;
            triangles.Add(previous);triangles.Add(current);triangles.Add(previous+1);
            triangles.Add(previous+1);triangles.Add(current);triangles.Add(current+1);
        }
        var mesh = new Mesh { name = "Menu edge falloff" };
        mesh.SetVertices(positions);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        meshes.Add(mesh);
        var go = new GameObject(top ? "Upper dark menu surround" : "Lower dark menu surround");
        go.layer=source.gameObject.layer;go.transform.SetParent(source.transform,true);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=material;
        objects.Add(go);
    }

    void OnDestroy()
    {
        Clear(); if (material != null) Destroy(material);
        if (source != null && originalMaterials != null) source.sharedMaterials=originalMaterials;
        if (backdropMaterials != null) foreach(Material backdrop in backdropMaterials) Destroy(backdrop);
    }
}

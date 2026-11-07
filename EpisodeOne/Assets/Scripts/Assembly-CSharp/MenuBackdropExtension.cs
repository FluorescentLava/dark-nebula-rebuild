using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Continue a narrow neutral edge band. Mirroring the complete menu artwork
// would repeat the baked title/banner at the top of a tall display.
public sealed class MenuBackdropExtension : MonoBehaviour
{
    public Camera Camera;
    readonly List<GameObject> objects = new List<GameObject>();
    readonly List<Mesh> meshes = new List<Mesh>();
    Renderer source;
    Vector3 nativeScale;
    bool hasNativeScale;
    float lastSize = -1f;
    float lastAspect = -1f;

    public static bool UsesMenuArtwork(Renderer backdrop)
    {
        return backdrop != null && backdrop.sharedMaterial != null &&
            backdrop.sharedMaterial.mainTexture != null && backdrop.sharedMaterial.mainTexture.name == "UI_MenuBackground";
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

    struct Vertex
    {
        public Vector3 point;
        public Vector2 uv;
        public Color color;
        public static Vertex Lerp(Vertex a, Vertex b, float t)
        {
            return new Vertex { point = Vector3.Lerp(a.point,b.point,t),
                uv = Vector2.Lerp(a.uv,b.uv,t), color = Color.Lerp(a.color,b.color,t) };
        }
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
        Rebuild();
    }

    void LateUpdate()
    {
        if (source != null && Camera != null &&
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
        if (source == null || Camera == null) return;
        if (!hasNativeScale)
        {
            nativeScale = source.transform.localScale;
            hasNativeScale = true;
        }
        // Other pages keep their larger control camera. Compensate only the
        // backdrop so the shared artwork and its edge bands project exactly
        // as they do behind the main menu.
        float mainMenuSize = LegacyScreenFit.MainMenuCameraSize(Camera);
        source.transform.localScale = nativeScale * (Camera.orthographicSize / mainMenuSize);
        if (Mathf.Abs(Vector3.Dot(source.bounds.center-Camera.transform.position,Camera.transform.up)) >
            source.bounds.extents.y+Camera.orthographicSize) return;
        Clear();
        MeshFilter filter = source.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;
        lastSize = Camera.orthographicSize; lastAspect = Camera.aspect;
        Build(filter.sharedMesh,true); Build(filter.sharedMesh,false);
    }

    static List<Vertex> Clip(List<Vertex> polygon, Vector3 up, float edge, bool above)
    {
        var result = new List<Vertex>();
        if (polygon.Count == 0) return result;
        Vertex previous = polygon[polygon.Count-1];
        float previousDistance = Vector3.Dot(previous.point,up)-edge;
        bool previousInside = above ? previousDistance >= 0f : previousDistance <= 0f;
        foreach (Vertex current in polygon)
        {
            float distance = Vector3.Dot(current.point,up)-edge;
            bool inside = above ? distance >= 0f : distance <= 0f;
            if (inside != previousInside)
                result.Add(Vertex.Lerp(previous,current,previousDistance/(previousDistance-distance)));
            if (inside) result.Add(current);
            previous = current; previousDistance = distance; previousInside = inside;
        }
        return result;
    }

    void Build(Mesh input, bool top)
    {
        Vector3 up = Camera.transform.up;
        Vector3[] original = input.vertices;
        Vector2[] uvs = input.uv;
        Color[] colors = input.colors;
        if (uvs.Length != original.Length) return;
        float min = float.PositiveInfinity, max = float.NegativeInfinity;
        var vertices = new Vertex[original.Length];
        for (int i=0;i<original.Length;i++)
        {
            Vector3 point = source.transform.TransformPoint(original[i]);
            float height = Vector3.Dot(point,up); min = Mathf.Min(min,height); max = Mathf.Max(max,height);
            vertices[i] = new Vertex { point = point, uv = uvs[i], color = colors.Length == original.Length ? colors[i] : Color.white };
        }
        float edge = top ? max : min;
        float band = (max-min)*.08f;
        if (band <= .001f) return;
        float visible = Vector3.Dot(Camera.transform.position,up)+(top ? 1f : -1f)*Camera.orthographicSize;
        int tiles = Mathf.Clamp(Mathf.CeilToInt((top ? visible-edge : edge-visible)/band),0,32);
        if (tiles == 0) return;
        var positions = new List<Vector3>(); var outputUV = new List<Vector2>(); var outputColors = new List<Color>();
        var submeshes = new List<int[]>();
        for (int submesh=0;submesh<input.subMeshCount;submesh++)
        {
            var triangles = new List<int>(); int[] indices = input.GetTriangles(submesh);
            for (int triangle=0;triangle<indices.Length;triangle+=3)
            {
                var polygon = new List<Vertex> { vertices[indices[triangle]],vertices[indices[triangle+1]],vertices[indices[triangle+2]] };
                polygon = Clip(polygon,up,top ? edge-band : edge,true);
                polygon = Clip(polygon,up,top ? edge : edge+band,false);
                if (polygon.Count < 3) continue;
                for (int tile=0;tile<tiles;tile++)
                {
                    int first = positions.Count; bool reflected = tile%2 == 0;
                    foreach (Vertex vertex in polygon)
                    {
                        float height = Vector3.Dot(vertex.point,up);
                        float mapped = reflected ? 2f*edge-height+(top ? 1f : -1f)*tile*band :
                            height+(top ? 1f : -1f)*(tile+1)*band;
                        positions.Add(vertex.point+(mapped-height)*up); outputUV.Add(vertex.uv); outputColors.Add(vertex.color);
                    }
                    for (int i=1;i<polygon.Count-1;i++)
                    {
                        triangles.Add(first); triangles.Add(first+(reflected ? i+1 : i)); triangles.Add(first+(reflected ? i : i+1));
                    }
                }
            }
            submeshes.Add(triangles.ToArray());
        }
        var mesh = new Mesh { name = "Neutral menu edge band" };
        mesh.SetVertices(positions); mesh.SetUVs(0,outputUV); mesh.SetColors(outputColors); mesh.subMeshCount = submeshes.Count;
        for (int i=0;i<submeshes.Count;i++) mesh.SetTriangles(submeshes[i],i);
        mesh.RecalculateBounds(); meshes.Add(mesh);
        var go = new GameObject(top ? "Upper neutral menu edge" : "Lower neutral menu edge");
        go.layer = source.gameObject.layer;
        // Visibility and menu transitions follow the original backdrop.
        go.transform.SetParent(source.transform,true);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = source.sharedMaterials;
        objects.Add(go);
    }

    void OnDestroy() { Clear(); }
}

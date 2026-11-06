using UnityEngine;
using UnityEngine.Rendering;

// Two cropped additions share the native road surface and its transform. Their
// binary masks replace only the missing sides and the adjoining forest edge.
[DefaultExecutionOrder(1500)]
public sealed class PortalJungleUnderlay : MonoBehaviour
{
    FlagConditionChanged activation, arrival;
    readonly GameObject[] planes = new GameObject[2];
    readonly Mesh[] meshes = new Mesh[2];
    readonly Material[] materials = new Material[2];
    readonly MeshRenderer[] patches = new MeshRenderer[2];
    Renderer road;

    void Awake()
    {
        ParallaxLayer layer = GetComponent<ParallaxLayer>();
        if (layer != null && layer.name == "aztek_walls" && Mathf.Approximately(layer.scrollValue, 1f))
        {
            foreach (CombineChildren batch in layer.GetComponentsInChildren<CombineChildren>())
            {
                if (!batch.name.StartsWith("aztek_walls_batch")) continue;
                Renderer candidate = batch.GetComponent<Renderer>();
                MeshFilter filter = batch.GetComponent<MeshFilter>();
                if (candidate != null && filter != null && filter.sharedMesh != null
                    && candidate.bounds.min.x <= 0f && candidate.bounds.max.x >= 0f
                    && candidate.bounds.min.z <= -51648f && candidate.bounds.max.z >= -51648f)
                { road = candidate; break; }
            }
        }
        Texture2D left = Resources.Load<Texture2D>("PortalJungleLeft");
        Texture2D right = Resources.Load<Texture2D>("PortalJungleRight");
        Shader shader = Resources.Load<Shader>("PortalJungleUnderlay");
        if (road == null || left == null || right == null || shader == null)
        {
            Debug.LogWarning("Portal jungle scenery needs the native stone bridge batch and both local resources.");
            enabled = false;
            return;
        }
        CreatePatch(0, "left", layer, left, shader, -256f, -128f);
        CreatePatch(1, "right", layer, right, shader, 128f, 288f);
        activation = new FlagConditionChanged(104);
        activation.Callback += Show;
        arrival = new FlagConditionChanged(105);
        arrival.Callback += Hide;
    }

    void CreatePatch(int index, string side, ParallaxLayer layer, Texture2D texture, Shader shader, float xmin, float xmax)
    {
        const float zmin = -51840f, zmax = -51584f;
        Material material = new Material(shader)
        {
            name = "Portal local jungle " + side, mainTexture = texture, renderQueue = 2991
        };
        GameObject plane = new GameObject("Portal jungle extension " + side);
        plane.layer = road.gameObject.layer;
        plane.transform.SetParent(road.transform, false);
        // Native tiles include a second layer-height offset before combining.
        float tileHeight = layer.transform.InverseTransformPoint(road.bounds.center).y;
        Vector3 centre = new Vector3(layer.MapWidth * 32f + (xmin + xmax) * .5f, tileHeight, (zmin + zmax) * .5f);
        plane.transform.localPosition = road.transform.InverseTransformPoint(layer.transform.TransformPoint(centre));
        plane.transform.localRotation = Quaternion.identity;
        plane.transform.localScale = Vector3.one;
        float halfWidth = (xmax - xmin) * .5f, halfLength = (zmax - zmin) * .5f;
        Mesh mesh = new Mesh { name = "Portal local jungle " + side + " plane" };
        mesh.vertices = new[] { new Vector3(-halfWidth,0,-halfLength), new Vector3(halfWidth,0,-halfLength), new Vector3(halfWidth,0,halfLength), new Vector3(-halfWidth,0,halfLength) };
        mesh.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        mesh.triangles = new[] { 0,2,1,0,3,2 };
        mesh.RecalculateBounds();
        plane.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = plane.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.enabled = false;
        planes[index] = plane; meshes[index] = mesh; materials[index] = material; patches[index] = renderer;
    }

    void LateUpdate()
    {
        // PortalEnvironment settles the native scenery queue first. Follow its
        // actual stage once it changes, without distance sorting or depth writes.
        if (road == null) return;
        int queue = road.sharedMaterial.renderQueue + 1;
        foreach (Material material in materials)
            if (material != null && material.renderQueue != queue) material.renderQueue = queue;
    }

    void Show() { if (isActiveAndEnabled) foreach (MeshRenderer patch in patches) if (patch != null) patch.enabled = true; }
    void Hide() { foreach (MeshRenderer patch in patches) if (patch != null) patch.enabled = false; }
    void OnDisable() { Hide(); }
    void OnDestroy()
    {
        Hide();
        if (activation != null) { activation.Callback -= Show; activation.Dispose(); }
        if (arrival != null) { arrival.Callback -= Hide; arrival.Dispose(); }
        foreach (GameObject plane in planes) if (plane != null) Destroy(plane);
        foreach (Mesh mesh in meshes) if (mesh != null) Destroy(mesh);
        foreach (Material material in materials) if (material != null) Destroy(material);
    }
}

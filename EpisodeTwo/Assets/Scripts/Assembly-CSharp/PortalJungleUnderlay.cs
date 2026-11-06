using UnityEngine;
using UnityEngine.Rendering;

// Additional artwork in the native stone bridge's coordinate system. Sharing
// its actual mesh plane and transform keeps the two surfaces locked in perspective.
public sealed class PortalJungleUnderlay : MonoBehaviour
{
    FlagConditionChanged activation, arrival;
    GameObject plane;
    Mesh mesh;
    Material material;
    MeshRenderer ground;

    void Awake()
    {
        ParallaxLayer layer = GetComponent<ParallaxLayer>();
        Renderer road = null;
        if (layer != null && layer.name == "aztek_walls" && Mathf.Approximately(layer.scrollValue, 1f))
        {
            foreach (CombineChildren batch in layer.GetComponentsInChildren<CombineChildren>())
            {
                if (!batch.name.StartsWith("aztek_walls_batch")) continue;
                Renderer candidate = batch.GetComponent<Renderer>();
                MeshFilter filter = batch.GetComponent<MeshFilter>();
                // Row 806 is on the long stone bridge, in the original forest batch.
                if (candidate != null && filter != null && filter.sharedMesh != null
                    && candidate.bounds.min.x <= 0f && candidate.bounds.max.x >= 0f
                    && candidate.bounds.min.z <= -51648f && candidate.bounds.max.z >= -51648f)
                { road = candidate; break; }
            }
        }
        Texture2D texture = Resources.Load<Texture2D>("PortalJungleUnderlayTexture");
        Shader shader = Resources.Load<Shader>("PortalJungleUnderlay");
        if (road == null || texture == null || shader == null)
        {
            Debug.LogWarning("Portal jungle scenery needs the native stone bridge batch and resources.");
            enabled = false;
            return;
        }
        material = new Material(shader)
        {
            name = "Portal jungle underlay instance", mainTexture = texture, renderQueue = 2980
        };
        plane = new GameObject("Portal jungle underlay");
        plane.layer = road.gameObject.layer;
        plane.transform.SetParent(road.transform, false);
        // Tiles carry a second layer-height offset before combining. Derive the
        // real surface from the native batch, rather than assuming the layer origin.
        float tileHeight = layer.transform.InverseTransformPoint(road.bounds.center).y;
        Vector3 centreInLayer = new Vector3(layer.MapWidth * 32f, tileHeight, -52096f);
        plane.transform.localPosition = road.transform.InverseTransformPoint(layer.transform.TransformPoint(centreInLayer));
        plane.transform.localRotation = Quaternion.identity;
        plane.transform.localScale = Vector3.one;
        mesh = new Mesh { name = "Portal jungle road-layer plane" };
        mesh.vertices = new[] { new Vector3(-512,0,-512), new Vector3(512,0,-512), new Vector3(512,0,512), new Vector3(-512,0,512) };
        mesh.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
        mesh.triangles = new[] { 0,2,1,0,3,2 };
        mesh.RecalculateBounds();
        plane.AddComponent<MeshFilter>().sharedMesh = mesh;
        ground = plane.AddComponent<MeshRenderer>();
        ground.sharedMaterial = material;
        ground.shadowCastingMode = ShadowCastingMode.Off;
        ground.receiveShadows = false;
        ground.lightProbeUsage = LightProbeUsage.Off;
        ground.reflectionProbeUsage = ReflectionProbeUsage.Off;
        ground.enabled = false;
        activation = new FlagConditionChanged(104);
        activation.Callback += Show;
        arrival = new FlagConditionChanged(105);
        arrival.Callback += Hide;
    }

    void Show() { if (isActiveAndEnabled && ground != null) ground.enabled = true; }
    void Hide() { if (ground != null) ground.enabled = false; }
    void OnDisable() { Hide(); }
    void OnDestroy()
    {
        Hide();
        if (activation != null) { activation.Callback -= Show; activation.Dispose(); }
        if (arrival != null) { arrival.Callback -= Hide; arrival.Dispose(); }
        if (plane != null) Destroy(plane);
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        // The imported texture is Resources-owned and is never mutated here.
    }
}

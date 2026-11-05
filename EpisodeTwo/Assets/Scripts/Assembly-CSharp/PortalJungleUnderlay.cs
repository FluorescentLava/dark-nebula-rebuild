using UnityEngine;
using UnityEngine.Rendering;

// Fixed additional ground for the tall-screen portal shot, below the native
// scenery. Native terrain meshes, materials and gameplay remain untouched.
public sealed class PortalJungleUnderlay : MonoBehaviour
{
    FlagConditionChanged activation, arrival;
    GameObject plane;
    Mesh mesh;
    Material material;
    MeshRenderer ground;

    void Awake()
    {
        Texture2D texture = Resources.Load<Texture2D>("PortalJungleUnderlayTexture");
        Shader shader = Resources.Load<Shader>("PortalJungleUnderlay");
        if (texture == null || shader == null)
        {
            Debug.LogWarning("Portal jungle scenery resources are missing.");
            enabled = false;
            return;
        }
        material = new Material(shader)
        {
            name = "Portal jungle underlay instance", mainTexture = texture, renderQueue = 1990
        };
        plane = new GameObject("Portal jungle underlay");
        plane.transform.SetParent(transform, false);
        plane.transform.position = new Vector3(0f, -1f, -52096f);
        plane.transform.rotation = Quaternion.identity;
        plane.transform.localScale = Vector3.one;
        mesh = new Mesh { name = "Portal jungle ground plane" };
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

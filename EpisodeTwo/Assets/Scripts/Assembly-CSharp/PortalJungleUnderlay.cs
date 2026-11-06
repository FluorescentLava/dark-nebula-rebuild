using UnityEngine;
using UnityEngine.Rendering;

// Additional artwork in the native stone bridge's coordinate system. Sharing
// its actual mesh plane and transform keeps the two surfaces locked in perspective.
public sealed class PortalJungleUnderlay : MonoBehaviour
{
    FlagConditionChanged activation, arrival;
    GameObject plane;
    Mesh mesh;
    Mesh cliffMesh;
    Material material;
    MeshRenderer ground;
    MeshRenderer cliff;

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
        // The raised camera sees past the original quad's forward edge at
        // z=-51584, including the exposed left side of the native forest.
        // Extend forward on this same plane, retaining 1024 world units per UV.
        // End one tile inside the forest front edge, preserving its transparent
        // transition into the Matrix above the forest.
        mesh.vertices = new[] { new Vector3(-512,0,-512), new Vector3(512,0,-512), new Vector3(512,0,1216), new Vector3(-512,0,1216) };
        mesh.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1.6875f), new Vector2(0,1.6875f) };
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
        AddNativeCliffEdge(layer);
        activation = new FlagConditionChanged(104);
        activation.Callback += Show;
        arrival = new FlagConditionChanged(105);
        arrival.Callback += Hide;
    }

    void AddNativeCliffEdge(ParallaxLayer layer)
    {
        Transform tileset = LevelAssets.GetAsset("tileset_aztek_walls");
        tk2dSprite sprite = tileset == null ? null : tileset.GetComponent<tk2dSprite>();
        if (sprite == null || sprite.collection == null) return;
        var vertices = new System.Collections.Generic.List<Vector3>();
        var uv = new System.Collections.Generic.List<Vector2>();
        var triangles = new System.Collections.Generic.List<int>();
        int[] columns = { 1, 2, 8, 9, 1, 2, 8, 9, 1, 2, 8, 9, 1, 2, 8, 9, 8, 9, 8, 9 };
        int[] rows = { 807, 807, 807, 807, 807, 807, 807, 807, 808, 808, 808, 808, 809, 809, 809, 809, 808, 808, 809, 809 };
        int[] sprites = { 176, 176, 180, 180, 252, 253, 254, 255, 176, 176, 180, 180, 192, 192, 196, 196, 254, 255, 238, 239 };
        Material source = null;
        // These four original atlas pieces finish the 236/237 and 238/239
        // cliff corners. Row 807 omits them outside the old narrow viewport.
        // Use the original artwork without editing its texture or mask.
        // The next two rows continue the adjacent ground outside the
        // old x=+/-160 crop. Each side uses its own adjacent atlas ground.
        // Reflect the first adjoining tile, then reflect it
        // back, so both shared edges sample exactly the same original pixels.
        // Draw the ground pieces first beneath the alpha cliffs. Their
        // lower edge matches row 808, hiding the unrelated large background
        // leaf that otherwise ends abruptly at that row's straight boundary.
        // The right cliff has a baked dark lower edge. Continue its original
        // two-row pattern in reverse below it, so that dark edge meets the
        // same pixels instead of an abrupt strip of brighter ground.
        for (int i = 0; i < columns.Length; i++)
        {
            tk2dSpriteDefinition definition = sprite.collection.spriteDefinitions[sprites[i]];
            int first = vertices.Count;
            Vector3 centre = new Vector3(columns[i] * 64f + 32f, layer.transform.position.y, -rows[i] * 64f - 32f);
            foreach (Vector3 point in definition.positions)
            {
                Vector3 nativePoint = point;
                if (i < 16 && (i < 4 || rows[i] >= 808) && (columns[i] == 2 || columns[i] == 8)) nativePoint.x = -nativePoint.x;
                if (i < 4 || i >= 16) nativePoint.y = -nativePoint.y;
                vertices.Add(plane.transform.InverseTransformPoint(layer.transform.TransformPoint(centre + Quaternion.LookRotation(Vector3.down) * nativePoint)));
            }
            uv.AddRange(definition.uvs);
            foreach (int index in definition.indices) triangles.Add(first + index);
            source = definition.material;
        }
        cliffMesh = new Mesh { name = "Portal native cliff edge mesh" };
        cliffMesh.SetVertices(vertices); cliffMesh.SetUVs(0, uv); cliffMesh.SetTriangles(triangles, 0); cliffMesh.RecalculateBounds();
        var edge = new GameObject("Portal native cliff edge");
        edge.layer = plane.layer; edge.transform.SetParent(plane.transform, false);
        edge.AddComponent<MeshFilter>().sharedMesh = cliffMesh;
        cliff = edge.AddComponent<MeshRenderer>(); cliff.sharedMaterial = layer.TileMaterial(source);
        cliff.shadowCastingMode = ShadowCastingMode.Off; cliff.receiveShadows = false;
        cliff.lightProbeUsage = LightProbeUsage.Off; cliff.reflectionProbeUsage = ReflectionProbeUsage.Off;
        cliff.enabled = false;
    }

    void Show() { if (isActiveAndEnabled) { if (ground != null) ground.enabled = true; if (cliff != null) cliff.enabled = true; } }
    void Hide() { if (ground != null) ground.enabled = false; if (cliff != null) cliff.enabled = false; }
    void OnDisable() { Hide(); }
    void OnDestroy()
    {
        Hide();
        if (activation != null) { activation.Callback -= Show; activation.Dispose(); }
        if (arrival != null) { arrival.Callback -= Hide; arrival.Dispose(); }
        if (plane != null) Destroy(plane);
        if (mesh != null) Destroy(mesh);
        if (cliffMesh != null) Destroy(cliffMesh);
        if (material != null) Destroy(material);
        // The imported texture is Resources-owned and is never mutated here.
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelEP2Runtime : ILevelEP2
{
	public const float GRIDSIZE = 64f;

	public const float HALFGRIDSIZE = 32f;

	public const float Y_ENTITIES = 32f;

	public List<TileMap> tilemaps;

	protected Vector2 tilemapCenterPosition;

	protected Vector2 layerScreenOffset;

	protected Vector2 cameraPosition;

	protected string levelPath;

	protected string sourcePath;

	protected string mapName;

	protected Dictionary<byte, CustomEntity> customEntities;

	public static Dictionary<string, TileRemapping> Remap;

	public int LevelNum;

	public int TopmostLayer;

	public int AbyssLayer;


	private static Dictionary<string, GameObject> BatchParents;

	static LevelEP2Runtime()
	{
		TileRemapping tileRemapping = new TileRemapping
		{
			Name = "aztek_walls",
			ReplacedName = "aztek_walls_replacement",
			Map = new Dictionary<ushort, ushort>
			{
				{ 9, 1 },
				{ 25, 5 },
				{ 40, 8 },
				{ 41, 9 },
				{ 56, 12 },
				{ 10, 2 },
				{ 26, 6 },
				{ 42, 10 },
				{ 58, 14 },
				{ 74, 3 }
			}
		};
		Remap = new Dictionary<string, TileRemapping> { { tileRemapping.Name, tileRemapping } };
	}

	public IEnumerator LoadAsync(MonoBehaviour monoBehaviour, int pLevelNum, Action pCallback)
	{
		levelPath = UserSettings.I.GetSceneName(pLevelNum) + "-level.level";
		if (levelPath.LastIndexOf("/") == -1)
		{
			sourcePath = string.Empty;
		}
		else
		{
			sourcePath = levelPath.Substring(0, levelPath.LastIndexOf("/"));
			sourcePath = sourcePath.Substring(0, sourcePath.LastIndexOf("/"));
		}
		LevelNum = pLevelNum;
		tilemaps = new List<TileMap>();
		yield return monoBehaviour.StartCoroutine(loadCustomEntities());
		DataInputStream stream = loadLevelDataFromName(levelPath, 258);
		tilemapCenterPosition = new Vector2(stream.ReadInt(), stream.ReadInt());
		resetScreenPositionOffset();
		byte layerCounts = (byte)(TopmostLayer = stream.ReadByte());
		for (byte i = 0; i < layerCounts; i++)
		{
			string layerName = stream.ReadString();
			float scrollSpeed = stream.ReadFloat();
			yield return monoBehaviour.StartCoroutine(LoadLayerAsync(monoBehaviour, layerName, i, scrollSpeed));
		}
		stream.Close();
		float delay = Time.realtimeSinceStartup + 0.5f;
		while (delay > Time.realtimeSinceStartup)
		{
			yield return 0;
		}
		if (pCallback != null)
		{
			pCallback();
		}
	}

	protected IEnumerator LoadLayerAsync(MonoBehaviour monoBehaviour, string layerName, int layerIndex, float scrollSpeed)
	{
		BatchParents = new Dictionary<string, GameObject>();
		TileMap map = loadTileMap(layerName);
		map.defineScrollSpeed(scrollSpeed);
		tilemaps.Add(map);
		tilemaps.Add(map);
		GameObject tiles = GameObject.Find("Tiles");
		GameObject layer = InstatiatePrefab("Assets/Prefabs/ParallaxLayer.prefab");
		layer.name = layerName;
        if(LevelNum!=1 && layerName=="aztek_walls")layer.AddComponent<BackdropRegionBounds>();
		ParallaxLayer parallax = layer.GetComponent<ParallaxLayer>();
		parallax.layerIndex = layerIndex;
		parallax.scrollValue = scrollSpeed;
		parallax.layerWidth = map.Width;
		parallax.Render = map.render;
		parallax.SetAlpha(map.alpha);
		parallax.SetBlend(map.blend);
		parallax.MapWidth = map.Width;
		parallax.MapHeight = map.Height;
		layer.transform.parent = tiles.transform;
		layer.transform.position = new Vector3(0f - map.origo.x, (float)layerIndex * 5f, 0f);
		if (parallax.Render)
		{
			addTileMapEntity(map, layer.transform, layerName, layerIndex);
		}
		loadCollision(layerName, map, layerIndex);
		GameObject colgroup = new GameObject("Collisions");
		colgroup.transform.parent = layer.transform;
		colgroup.transform.position = Vector3.zero;
		yield return 0;
		WallEntityCreatorBlocks.spawnTileMapCollisionEntities(this, map, layerIndex, colgroup);
		yield return monoBehaviour.StartCoroutine(LoadEntitiesAsync(monoBehaviour, layerName, layerIndex, map.Width, map.Height, layer.transform, !parallax.isNoScrollLayer));
	}

	private static void BatchAdd(GameObject go, string name, Transform parent)
	{
		int num = Mathf.FloorToInt(go.transform.position.z / 640f);
		GameObject gameObject = null;
		string text = name + num;
		if (BatchParents.ContainsKey(text))
		{
			gameObject = BatchParents[text];
		}
		if (gameObject == null)
		{
			gameObject = new GameObject(text);
			gameObject.transform.parent = parent;
			gameObject.AddComponent<CombineChildren>();
			gameObject.isStatic = true;
			BatchParents.Add(text, gameObject);
		}
		go.transform.parent = gameObject.transform;
	}

    private static void InsetForestTileUvs(tk2dSprite sprite)
    {
        var filter = sprite.GetComponent<MeshFilter>();
        var texture = sprite.GetComponent<Renderer>().sharedMaterial.mainTexture;
        if (filter == null || filter.sharedMesh == null || texture == null) return;
        Mesh mesh = filter.sharedMesh; // tk2dSprite owns a separate mesh per instance.
        Vector2[] uv = mesh.uv;
        if (uv.Length == 0) return;
        Vector2 minimum = uv[0], maximum = uv[0];
        foreach (Vector2 point in uv) { minimum = Vector2.Min(minimum, point); maximum = Vector2.Max(maximum, point); }
        Vector2 center = (minimum + maximum) * .5f;
        Vector2 span = maximum - minimum;
        if (span.x <= 1f / texture.width || span.y <= 1f / texture.height) return;
        // Recovered UVs run almost exactly along pixel borders. Bilinear
        // sampling then mixes the black packing gutter into adjoining tiles.
        Vector2 scale = new Vector2(1f - 1f / (texture.width * span.x), 1f - 1f / (texture.height * span.y));
        for (int i = 0; i < uv.Length; i++) uv[i] = center + Vector2.Scale(uv[i] - center, scale);
        mesh.uv = uv;
    }

	private IEnumerator loadCustomEntities()
	{
		customEntities = new Dictionary<byte, CustomEntity>();
		DataInputStream stream = DataStreamManager.LoadResourceStream("entity.type", 257);
		byte count = stream.ReadByte();
		float yieldTime = Time.realtimeSinceStartup + 0.3f;
		for (byte i = 0; i < count; i++)
		{
			byte typeId = stream.ReadByte();
			byte inheritedTypeId = stream.ReadByte();
			byte parameterCount = stream.ReadByte();
			Dictionary<string, string> properties = new Dictionary<string, string>();
			for (int j = 0; j < parameterCount; j++)
			{
				string key = stream.ReadString();
				string v = stream.ReadString();
				properties[key] = DataStreamManager.convertEnumStringsToValue(v);
			}
			customEntities[typeId] = new CustomEntity(properties, inheritedTypeId);
			if (yieldTime < Time.realtimeSinceStartup)
			{
				yieldTime = Time.realtimeSinceStartup + 0.3f;
				yield return 0;
			}
		}
		copyFromInheritedEntities();
	}

	private void copyFromInheritedEntities()
	{
		for (int i = 0; i < 256; i++)
		{
			CustomEntity customEntity = null;
			if (customEntities.ContainsKey((byte)i))
			{
				customEntity = customEntities[(byte)i];
			}
			if (customEntity != null)
			{
				copyFromInheritedEntity(customEntity, customEntity.inheritedFromId);
			}
		}
	}

	private void copyFromInheritedEntity(CustomEntity target, byte inheritedFromId)
	{
		if (inheritedFromId > 127)
		{
			CustomEntity customEntity = customEntities[inheritedFromId];
			target.copyFromInherited(customEntity);
			copyFromInheritedEntity(target, customEntity.inheritedFromId);
		}
		else
		{
			target.setBasedOnEntityTypeId(inheritedFromId);
		}
	}

	protected void loadCollision(string layerName, TileMap map, int layerIndex)
	{
		string pResourcePath = "Collisions/" + layerName + ".collision";
		DataInputStream dataInputStream = DataStreamManager.LoadResourceStream(pResourcePath, 257);
		byte b = dataInputStream.ReadByte();
		for (byte b2 = 0; b2 < b; b2++)
		{
			ushort tileIndex = dataInputStream.ReadUInt16();
			byte b3 = dataInputStream.ReadByte();
			for (byte b4 = 0; b4 < b3; b4++)
			{
				byte b5 = dataInputStream.ReadByte();
				TypeCollisionInfoType typeCollisionInfoType = TypeCollisionInfoType.none;
				TileCollisionInfo collisionInfo = null;
				switch (b5)
				{
				case 1:
					typeCollisionInfoType = TypeCollisionInfoType.boxWall;
					collisionInfo = loadCollisionBox(typeCollisionInfoType, dataInputStream);
					break;
				case 6:
					typeCollisionInfoType = TypeCollisionInfoType.abyss;
					collisionInfo = loadCollisionBox(typeCollisionInfoType, dataInputStream);
					if (AbyssLayer < layerIndex)
					{
						AbyssLayer = layerIndex;
					}
					break;
				case 7:
					typeCollisionInfoType = TypeCollisionInfoType.outOfWorld;
					collisionInfo = loadCollisionBox(typeCollisionInfoType, dataInputStream);
					break;
				case 8:
					typeCollisionInfoType = TypeCollisionInfoType.water;
					collisionInfo = loadCollisionBox(typeCollisionInfoType, dataInputStream);
					if (AbyssLayer < layerIndex)
					{
						AbyssLayer = layerIndex;
					}
					break;
				case 2:
					typeCollisionInfoType = TypeCollisionInfoType.circleWall;
					collisionInfo = loadCollisionCircle(typeCollisionInfoType, dataInputStream);
					break;
				case 3:
					typeCollisionInfoType = TypeCollisionInfoType.death;
					collisionInfo = loadCollisionBox(typeCollisionInfoType, dataInputStream);
					break;
				case 4:
					switch (dataInputStream.ReadByte())
					{
					case 0:
						typeCollisionInfoType = TypeCollisionInfoType.redEnergySource;
						break;
					case 1:
						typeCollisionInfoType = TypeCollisionInfoType.greenEnergySource;
						break;
					case 2:
						typeCollisionInfoType = TypeCollisionInfoType.blueEnergySource;
						break;
					default:
						Debug.LogError("Collision: " + typeCollisionInfoType);
						break;
					}
					collisionInfo = loadCollisionBox(typeCollisionInfoType, dataInputStream);
					break;
				default:
					Debug.LogError("Illegal shape type: " + b5);
					break;
				}
				map.addTileCollisionInfo(collisionInfo, tileIndex);
			}
		}
	}

	private TileCollisionInfo loadCollisionBox(TypeCollisionInfoType type, DataInputStream stream)
	{
		Vector2 pPos = stream.ReadPosition();
		float pWidth = stream.ReadFloat();
		float pHeight = stream.ReadFloat();
		return new TileCollisionInfo(type, pPos, pWidth, pHeight);
	}

	private TileCollisionInfo loadCollisionCircle(TypeCollisionInfoType type, DataInputStream stream)
	{
		Vector2 pPos = stream.ReadPosition();
		float num = stream.ReadFloat();
		return new TileCollisionInfo(type, pPos, num, num);
	}

	private IEnumerator LoadEntitiesAsync(MonoBehaviour monoBehaviour, string layerName, int layerIndex, ushort tileWidth, ushort tileHeight, Transform parent, bool isScollingLayer)
	{
		DataInputStream stream = loadLevelDataFromName(levelPath.Replace("level.level", layerName + ".entities"), 259);
		GameObject entitiesGroup = GameObject.Find("Entities_" + layerName);
		if (entitiesGroup == null)
		{
			entitiesGroup = new GameObject("Entities_" + layerName);
			entitiesGroup.transform.parent = parent;
			entitiesGroup.transform.localPosition = new Vector3(0f, layerIndex * 5 + 1, 0f);
		}
		float yieldTime = Time.realtimeSinceStartup + 0.3f;
		ushort entityCount = stream.ReadUInt16();
		for (ushort i = 0; i < entityCount; i++)
		{
			ushort x = stream.ReadUInt16();
			int y = stream.ReadInt();
			Vector2 worldPosition = new Vector2((int)x, y);
			byte entityTypeId = stream.ReadByte();
			Entity entity = CreateEntity(entityTypeId, worldPosition.x, 0f - worldPosition.y, entitiesGroup.transform);
			try
			{
				DataStreamManager.readEntityParametersForObject(entity, stream);
			}
			catch (Exception ex)
			{
				Debug.LogError(string.Concat("Failed to read entity parameter ", entity, " y", 0f - worldPosition.y, " ", ex.Message, "\n\n", ex.StackTrace));
			}
			if (entity != null)
			{
				entity.SetParameter("layer", layerIndex.ToString());
				entity.isInScollingLayer = isScollingLayer;
				entity.InitParametersInRuntime();
				entity.PostInit();
				if (entity.GetParameterInt("autochild") > 0)
				{
					Entity child = CreateEntity((ushort)entity.GetParameterInt("autochild"), 0f, 0f, entity.transform);
					child.transform.localPosition = new Vector3(entity.GetParameterFloat("autochild_offsetX"), entity.GetParameterFloat("autochild_relativelayer") * 5f, 0f - entity.GetParameterFloat("autochild_offsetY"));
				}
			}
			if (yieldTime < Time.realtimeSinceStartup)
			{
				yieldTime = Time.realtimeSinceStartup + 0.3f;
				yield return 0;
			}
		}
	}

	private string GetEntityName(ushort id)
	{
		switch (id)
		{
		case 1:
			return "PuckEntity";
		case 2:
			return "StandardVesselEntity";
		case 3:
			return "LaserDoorEntity";
		case 4:
			return "LaserDoorVerticalEntity";
		case 5:
			return "LifeOrbEntity";
		case 6:
			return "DoorEntity";
		case 7:
			return "BumpEntity";
		case 8:
			return "PuckSlotEntity";
		case 9:
			return "WindmillEntity";
		case 10:
			return "EndOfLevelEntity";
		case 11:
			return "WellDoneEntity";
		case 12:
			return "CheckpointEntity";
		case 13:
			return "CheckpointEntity";
		case 14:
			return "IntroShuttleEntity";
		case 15:
			return "OutroShuttleEntity";
		case 16:
			return "SpeedChangeEntity";
		case 17:
			return "MovingObstacleEntity";
		case 18:
			return "TurretEntity";
		case 19:
			return "MovingObstacleVerticalEntity";
		case 20:
			return "SuperLifeOrbEntity";
		case 21:
			return "MatrixChaserSpawnPointEntity";
		case 22:
			return "FireWallEntity";
		case 23:
			return "EndOfGameEntity";
		case 24:
			return "MainframeEntity";
		case 25:
			return "BrokenMainframeEntity";
		case 26:
			return "EndShipEntity";
		case 27:
			return "MainframeTopEntity";
		case 28:
			return "BattlePuckEntity";
		case 29:
			return "ChainsawPuckEntity";
		case 30:
			return "ChargePuckEntity";
		case 31:
			return "WindmillPuckEntity";
		case 32:
			return "LayerFaderEntity";
		case 33:
			return "MoviePlayerEntity";
		case 34:
			return "LargeBushEntity";
		case 35:
			return "SingleBushEntity";
		case 36:
			return "MusicPlayerEntity";
		case 37:
			return "MagnetVesselSlotEntity";
		case 38:
			return "ScrollingTexture";
		case 39:
			return "EnemySpawnerEntity";
		case 40:
			return "OutOfWorldEntity";
		case 41:
			return "GeneratorEntity";
		case 42:
			return "VesselBulletActivatorEntity";
		case 43:
			return "AnimatedEntity";
		case 44:
			return "VesselRespawnEntity";
		case 45:
			return "VesselTriggerEntity";
		case 46:
			return "LayerToggleEntity";
		case 47:
			return "AnimatedCameraEntity";
		case 48:
			return "VesselDespawnEntity";
		case 49:
			return "SpeedPadEntity";
		case 50:
			return "VesselBulletSpeedUpActivatorEntity";
		case 51:
			return "TheMerdinatorEntity";
		case 52:
			return "SoundEntity";
		case 53:
			return "ScreenWhiteOutEntity";
		case 54:
			return "ShooterEnemyWaveEntity";
		case 55:
			return "StarfighterBackgroundEntity";
		case 56:
			return "StarfighterShieldPickupEntity";
		case 57:
			return "StarfighterWeaponsPickupEntity";
		case 58:
			return "StarfighterVesselRespawnEntity";
		case 59:
			return "MurtyHellicasterEntity";
		case 60:
			return "ShooterBoss";
		case 61:
			return "ShooterCameraStopperEntity";
		case 62:
			return "DarknessEntity";
		case 63:
			return "DN2WindmillEntity";
		case 64:
			return "FlareEntity";
		case 65:
			return "HUDMessageEntity";
		case 66:
			return "ImpenetrableWallEntity";
		case 67:
			return "TreadmillEntity";
		case 68:
			return "LightEntity";
		case 69:
			return "LightConeEntity";
		case 70:
			return "PushButtonEntity";
		case 71:
			return "FlipFloorEntity";
		case 72:
			return "ButtonPuckEntity";
		case 73:
			return "DisableAlwaysAliveAndKeepaliveEntity";
		case 74:
			return "Cthulhu";
		case 75:
			return "Fish";
		case 76:
			return "HomingTurretEntity";
		case 77:
			return "ElectroPuck";
		case 78:
			return "ElectroPuckSlot";
		case 79:
			return "BoostShotActivator";
		default:
			return "!Unknown " + id;
		}
	}

	private Entity CreateEntity(ushort id, float x, float y, Transform parent)
	{
		if (id >= 128)
		{
			CustomEntity customEntity = customEntities[(byte)id];
			if (customEntity == null)
			{
				Debug.LogError(id + " is not a custom entity");
				return null;
			}
			Entity entity = CreateBaseEntity(customEntity.basedOnEntityTypeId, x, y, parent);
			if (entity == null)
			{
				return null;
			}
			for (int i = 0; i < customEntity.PropertyNames.Count; i++)
			{
				entity.SetParameter(customEntity.PropertyNames[i], customEntity.PropertyNormal[i], customEntity.PropertyExpert[i]);
			}
			return entity;
		}
		return CreateBaseEntity(id, x, y, parent);
	}

	private Entity CreateBaseEntity(ushort id, float x, float y, Transform parent)
	{
		string entityName = GetEntityName(id);
		GameObject gameObject = InstatiatePrefab("Assets/Prefabs/Entities/" + entityName + ".prefab");
		if (gameObject == null)
		{
			Debug.LogWarning("Missing entity " + entityName);
			gameObject = InstatiatePrefab("Assets/Prefabs/Entities/MissingEntity.prefab");
		}
		gameObject.transform.parent = parent;
		gameObject.transform.localPosition = new Vector3(x, 0f, y);
		if (entityName == "Fish")
		{
			gameObject.transform.rotation = Quaternion.identity;
		}
		else
		{
			gameObject.transform.rotation = Quaternion.LookRotation(Vector3.down);
		}
		gameObject.name = entityName;
		Entity component = gameObject.GetComponent<Entity>();
		if (component != null)
		{
			component.ResetParameters();
			component.isCreatedFromLevel = true;
			component.enabled = true;
		}
		return component;
	}

	public static string GetNameFromPath(string path)
	{
		path = path.Replace("/", "\\");
		int num = path.LastIndexOf("\\");
		string text = ((num != -1) ? path.Substring(num + 1) : path);
		return text.Substring(0, text.LastIndexOf("."));
	}

	public static GameObject Instatiate(string path)
	{
		string nameFromPath = GetNameFromPath(path);
		Transform asset = LevelAssets.GetAsset(nameFromPath);
		if (asset == null)
		{
			Debug.LogError("Coult not LevelAssets.GetAsset(" + path + " : " + nameFromPath + ")");
			return null;
		}
		GameObject gameObject = (UnityEngine.Object.Instantiate(asset) as Transform).gameObject;
		if (gameObject == null)
		{
			Debug.LogError("Coult not LevelAssets.GetAsset(" + path + " : " + nameFromPath + ") gameObject");
			return null;
		}
		return gameObject;
	}

	public GameObject InstatiatePrefab(string path)
	{
		return Instatiate(path);
	}

	protected GameObject GetTilemapAsset(string name)
	{
		Transform asset = LevelAssets.GetAsset(name);
		if (asset == null)
		{
			Debug.LogWarning("Could not LevelAssets.GetAsset(" + name + ") perhaps not even needed...");
			return null;
		}
		return asset.gameObject;
	}

	protected virtual void addTileMapEntity(TileMap tileMap, Transform pLayer, string layerName, int layerIndex)
	{
		bool tallViewport = (float)Screen.width / Mathf.Max(1, Screen.height)
			< LegacyScreenFit.ReferenceAspect - .001f;
		string text = "tileset_" + layerName;
		GameObject tilemapAsset = GetTilemapAsset(text);
		GameObject gameObject = null;
		TileRemapping value = null;
		if (Remap.TryGetValue(layerName, out value))
		{
			gameObject = GetTilemapAsset("tileset_" + value.ReplacedName);
			if (gameObject == null)
			{
				Debug.LogWarning("Could not find replaceTilemap for " + text + " " + value.ReplacedName);
				value = null;
			}
		}
		if (!(tilemapAsset != null))
		{
			return;
		}
		for (ushort num = 0; num < tileMap.Height; num++)
		{
			for (ushort num2 = 0; num2 < tileMap.Width; num2++)
			{
				ushort num3 = tileMap.GetTile(num2, num);
				if (num3 != ushort.MaxValue)
				{
					GameObject gameObject2 = null;
					if (value != null)
					{
						int tile = value.GetTile(num3);
						if (tile != -1)
						{
							gameObject2 = UnityEngine.Object.Instantiate(gameObject) as GameObject;
							num3 = (ushort)tile;
						}
					}
					if (gameObject2 == null)
					{
						gameObject2 = UnityEngine.Object.Instantiate(tilemapAsset) as GameObject;
					}
					gameObject2.name = "t";
					gameObject2.transform.rotation = Quaternion.LookRotation(Vector3.down);
					gameObject2.transform.parent = pLayer.transform;
					gameObject2.transform.localPosition = new Vector3((float)(int)num2 * 64f + 32f, pLayer.position.y, (float)(-num) * 64f - 32f);
					tk2dSprite component = gameObject2.GetComponent<tk2dSprite>();
					component.spriteId = num3;
                    if (LevelNum == 1 && layerName == "matrix_overlay" && num == 794
                        && tallViewport)
                    {
                        // The recovered header contains a white padding tile.
                        // The extended neutral grid replaces this unused cap.
                        UnityEngine.Object.Destroy(gameObject2);
                        continue;
                    }
                    if (LevelNum == 1 && tallViewport
                        && (layerName == "aztek_undergrowth" || (layerName == "aztek_walls" && num >= 794 && num <= 806)
                            || (layerName == "matrix_overlay" && num >= 795 && num <= 806)))
                        InsetForestTileUvs(component);
					string batchName = layerName + "_batch";
                    if (LevelNum == 1 && layerName.StartsWith("aztek_") && pLayer.GetComponent<ParallaxLayer>().scrollValue == 1f && num < 794)
                        batchName = "MatrixSide_" + batchName;
                    // Keep the recovered upper-forest blend separate from the
                    // native stone ring and the newer lower scenery patches.
                    if (LevelNum == 1 && num >= 794 && num <= 797 && layerName == "aztek_walls"
                        && tallViewport)
                        batchName = "ForestPortalCap_" + batchName;
                    BatchAdd(gameObject2, batchName, pLayer);
				}
			}
		}
        if (LevelNum == 1)
        {
            if (layerName == "matrix_overlay" && tallViewport)
            {
                // Extend only the portal's neutral perimeter. The two earlier
                // scan patches (rows 820/859) and the central ring are unchanged.
                const int first = 795, last = 806, columns = 12, rows = 12;
                for (int row = first - rows; row <= last + rows; row++)
                for (int column = 1 - columns; column <= 9 + columns; column++)
                {
                    if (row >= first && row <= last && column >= 1 && column <= 9) continue;
                    // Stop before the independently authored first scan patch.
                    if (row >= 820) continue;
                    int sourceRow = row; bool flipY = false;
                    if (row < first)
                    {
                        int phase = (first - row - 1) % 6;
                        sourceRow = first + (phase < 3 ? phase : 5 - phase);
                        flipY = phase < 3;
                    }
                    else if (row > last)
                    {
                        int phase = (row - last - 1) % 4;
                        sourceRow = last - (phase < 2 ? phase : 3 - phase);
                        flipY = phase < 2;
                    }
                    int sourceColumn = Mathf.Clamp(column, 1, 9);
                    bool flipX = column < 1 ? (1 - column) % 2 == 1 : column > 9 && (column - 9) % 2 == 1;
                    ushort id = tileMap.GetTile((ushort)sourceColumn, (ushort)sourceRow);
                    if (id == TileMap.Empty) continue;
                    var extension = UnityEngine.Object.Instantiate(tilemapAsset);
                    extension.transform.SetParent(pLayer, false);
                    extension.transform.rotation = Quaternion.LookRotation(Vector3.down);
                    extension.transform.localPosition = new Vector3(column * 64f + 32f, pLayer.position.y, -row * 64f - 32f);
                    extension.transform.localScale = new Vector3(flipX ? -1 : 1, flipY ? -1 : 1, 1);
                    extension.GetComponent<tk2dSprite>().spriteId = id;
                    InsetForestTileUvs(extension.GetComponent<tk2dSprite>());
                    BatchAdd(extension, "PortalOverlayExtension", pLayer);
                }
            }
            if (layerName == "aztek_undergrowth" && tallViewport)
            {
                int first = 0;
                while (first < tileMap.Height && tileMap.GetTile((ushort)(tileMap.Width / 2), (ushort)first) == TileMap.Empty) first++;
                if (first + 20 > tileMap.Height) return;
                // Restore the complete authored eight-row foliage cycle from
                // eab4ef3. Padding on both sides also covers the rotating shot.
                for (int row = first - 32; row < first + 16; row++)
                for (int column = -8; column < tileMap.Width + 8; column++)
                {
                    if (row >= first && column >= 1 && column <= 5) continue;
                    ushort srcX = (ushort)(1 + ((column - 1) % 5 + 5) % 5);
                    ushort srcY = (ushort)(first + 12 + ((row - first) % 8 + 8) % 8);
                    GameObject tile = UnityEngine.Object.Instantiate(tilemapAsset);
                    tile.transform.SetParent(pLayer, false);
                    tile.transform.rotation = Quaternion.LookRotation(Vector3.down);
                    tile.transform.localPosition = new Vector3(column * 64f + 32f, pLayer.position.y, -row * 64f - 32f);
                    tile.GetComponent<tk2dSprite>().spriteId = tileMap.GetTile(srcX, srcY);
                    InsetForestTileUvs(tile.GetComponent<tk2dSprite>());
                    BatchAdd(tile, "ForestCanopyPadding", pLayer);
                }
            }
            return;
        }
        TileMap nextForest=null;
        if(layerName=="matrix_floor" && Resources.Load<TextAsset>(levelPath.Replace("level.level","aztek_floor.tilemap"))!=null)
            nextForest=loadTileMap("aztek_floor");
        foreach(BackgroundTileExtension.Tile tile in BackgroundTileExtension.Tiles(tileMap,layerName,nextForest))
        {
            GameObject extension=UnityEngine.Object.Instantiate(tilemapAsset);
            extension.name="Backdrop extension";
            extension.transform.rotation=Quaternion.LookRotation(Vector3.down);
            extension.transform.parent=pLayer;
            extension.transform.localPosition=new Vector3(tile.x*64f+32f,pLayer.position.y,-tile.y*64f-32f);
            extension.GetComponent<tk2dSprite>().spriteId=tile.id;
            BatchAdd(extension,layerName+"_extension_batch",pLayer);
        }

	}

	protected TileMap loadTileMap(string layerName)
	{
		string path = levelPath.Replace("level.level", layerName + ".tilemap");
		DataInputStream dataInputStream = loadLevelDataFromName(path, 256);
		ushort num = dataInputStream.ReadUInt16();
		ushort num2 = dataInputStream.ReadUInt16();
		TileMap tileMap = new TileMap(num, num2);
		for (ushort num3 = 0; num3 < num2; num3++)
		{
			for (ushort num4 = 0; num4 < num; num4++)
			{
				ushort tileIndex = dataInputStream.ReadUInt16();
				tileMap.setTileAt(num4, num3, tileIndex);
			}
		}
		bool render;
		bool blend;
		if (dataInputStream.HasReachedEnd)
		{
			render = true;
			blend = true;
		}
		else
		{
			render = dataInputStream.ReadUInt16() != 0;
			blend = dataInputStream.ReadUInt16() != 0;
		}
		tileMap.render = render;
		tileMap.blend = blend;
		dataInputStream.Close();
		return tileMap;
	}

	private DataInputStream loadLevelDataFromName(string path, ushort version)
	{
		return DataStreamManager.LoadResourceStream(path, version);
	}

	private void resetScreenPositionOffset()
	{
		layerScreenOffset.x = 0f - cameraPosition.x + 160f;
		layerScreenOffset.y = 0f - cameraPosition.y + 240f;
	}
}

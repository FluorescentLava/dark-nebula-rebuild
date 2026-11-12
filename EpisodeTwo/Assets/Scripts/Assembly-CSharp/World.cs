using System;
using System.Collections;
using System.Collections.Generic;
using FLD;
using UnityEngine;

public class World : MonoBehaviour
{
    static Camera visibleCamera;
    public static Vector2 VisibleHalfSize
    {
        get
        {
            if(visibleCamera==null && cameraFollow!=null)visibleCamera=cameraFollow.GetComponent<Camera>();
            if(visibleCamera==null)return new Vector2(160f,240f);
            return new Vector2(visibleCamera.orthographicSize*visibleCamera.aspect,visibleCamera.orthographicSize);
        }
    }
	public const float FreezeMargin = 600f;

	public const float DefrostMargin = 500f;

	private static Dictionary<int, FlagSignal> FlagSignals = new Dictionary<int, FlagSignal>();

	public static Vessel vessel;

	public static CameraFollow cameraFollow;

	public static CameraManager CameraManager;

	public static ScriptManager ScriptManager;

	public static FreezeManager FreezeManager;

	public static float startTime = 0f;

	public static Hud hud;

	public static SkillMeter skillMeter;

	public static bool isInMatrixMode = false;

	public static bool isEndOfMatrix = false;

	public static int levelNum = 1;

	public static int mapHeight = 1000;

	public static int lifeOrbsOnLevel = 0;

	public static DarknessEntity Darkness;

	private static bool _isRunning = false;

	public int LevelNum = 1;

	public int MapHeight = 1000;

	public bool MatrixMode;

	public int AbyssLayer;

	public int TopmostLayer;

	public bool LoadLevelAtRuntime;

	public GameObject ShockwaveEntity;

	public GameObject ParticleEntity;

	public AudioClip SfxExpertMode;

	public AudioClip SfxIntro;

	private AudioClip Music;

	public AudioClip[] MusicClips;

	public static World _world;

	private static CameraFade fade;

	private static Hashtable Flags;

	public static float distortion = 0f;

	public static Shader distortionShader;

	public Shader DistortionShader;

	private static List<ParallaxLayer> Layers;

	private TempleEffect templeEffect;

	private ScreenWhiteOut screenWhiteOut;

	private bool _startedFadeIn;

	public static bool IsRestart = false;

	private FlagConditionChanged _flagEndGame;

	private static float _difficulty;

	private static float animationTimeOffset;

	private static bool hasAnimationDifficulty;

	public static bool IsRunning
	{
		get
		{
			return _isRunning;
		}
	}

	public static float WorldTimeInSeconds
	{
		get
		{
			return Time.time - startTime;
		}
	}

	// Difficulty changes affect future motion without rephasing every mechanism.
	public static float AnimationTimeInSeconds
	{
		get { return WorldTimeInSeconds * _difficulty + animationTimeOffset; }
	}

	public static float Difficulty
	{
		get
		{
			return _difficulty;
		}
	}

	public static ParallaxLayer TopLayer
	{
		get
		{
			ParallaxLayer parallaxLayer = null;
			foreach (ParallaxLayer layer in Layers)
			{
				if (parallaxLayer == null || layer.transform.position.y > parallaxLayer.transform.position.y)
				{
					parallaxLayer = layer;
				}
			}
			return parallaxLayer;
		}
	}

	public static event Action SignalUpdate;

	public static event Action<float> SignalDifficultyChanged;

	public static event Action SignalVesselDied;

	public static event Action GamePreStart;

	public static event Action OnDisableAlwaysAliveAndKeepaliveEntities;

	public static void Trigger(int pFlag, int pValue)
	{
		GetTrigger(pFlag).Trigger(pValue);
	}

	public static FlagSignal GetTrigger(int pFlag)
	{
		FlagSignal value;
		if (!FlagSignals.TryGetValue(pFlag, out value))
		{
			value = new FlagSignal();
			FlagSignals.Add(pFlag, value);
		}
		return value;
	}

	public static void GamePreStartOnce(Action Callback)
	{
		AddOnce(GamePreStart, Callback);
	}

	private static void AddOnce(Action Signal, Action Callback)
	{
		Action handler = null;
		handler = () =>
		{
			Signal = (Action)Delegate.Remove(Signal, handler);
			Callback();
		};
		Signal = (Action)Delegate.Combine(Signal, handler);
	}

	public static void ResetFlag(int pFlag)
	{
		Flags[pFlag] = 0;
	}

	public void AddMusicClip(string pMusicName)
	{
	}

	public static World GetWorldInstance()
	{
		GameObject gameObject = GameObject.Find("!World");
		if (gameObject == null)
		{
			Debug.LogWarning("!World not found");
			return null;
		}
		World component = gameObject.GetComponent<World>();
		if (component == null)
		{
			Debug.LogWarning("World behaviour not found in !World");
		}
		return component;
	}

	private void Awake()
	{
		DarkNebulaMetrics.StartLevel(levelNum, UserSettings.I.expert);
		DarkNebulaMetrics.StartLevelScreen(DarkNebulaMetrics.LevelScreens.Level_, LevelNum);
		SleepTimeoutManager.NeverSleep();
		Audio.ClearAllSounds();
		Time.timeScale = 0f;
		_world = this;
		lifeOrbsOnLevel = 0;
		levelNum = LevelNum;
		mapHeight = MapHeight;
		isInMatrixMode = MatrixMode;
		distortionShader = DistortionShader;
		isEndOfMatrix = false;
		CameraManager = base.gameObject.GetOrAddComponent<CameraManager>();
		Flags = new Hashtable();
		skillMeter = new SkillMeter();
		if (screenWhiteOut == null)
		{
			screenWhiteOut = base.gameObject.AddComponent<ScreenWhiteOut>();
		}
		FreezeManager = base.gameObject.GetOrAddComponent<FreezeManager>();
		cameraFollow = GameObject.Find("Main Camera").GetComponent<CameraFollow>();
		cameraFollow.GetComponent<Camera>().enabled = false;
		hud = GameObject.Find("Hud").GetComponent<Hud>();
		hud.ShowHudIcons(false);
		_flagEndGame = new FlagConditionChanged(95);
		_flagEndGame.Callback += GameCompleted;
		if (LoadLevelAtRuntime)
		{
			LevelEP2Runtime levelEP2Runtime = new LevelEP2Runtime();
			StartCoroutine(levelEP2Runtime.LoadAsync(this, LevelNum, () =>
			{
				InitWorld();
			}));
		}
		else
		{
			InitWorld();
		}
	}

	protected void InitWorld()
	{
        if (LevelNum == 1) gameObject.AddComponent<PortalEnvironment>();

		fade = base.gameObject.GetOrAddComponent<CameraFade>();
		fade.SetScreenOverlayColor(Color.black);
		hud.ShowHudIcons();
		ScriptManager = new ScriptManager();
		ScriptLoader scriptLoader = new ScriptLoader(ScriptManager);
		scriptLoader.LoadScripts(LevelNum);
		scriptLoader.Dispose();
		string introMusic = ScriptManager.GetIntroMusic();
		Music = introMusic != null ? GetMusicClip(introMusic) : null;
		cameraFollow.GetComponent<Camera>().enabled = true;
		startTime = Time.time;
		animationTimeOffset = 0f;
		hasAnimationDifficulty = false;
		templeEffect = new TempleEffect();
		if (GamePreStart != null)
		{
			GamePreStart();
		}
		_isRunning = true;
		GC.Collect();
		StartFadeIn();
	}

	private void ChartBoostWasViewed()
	{
		Debug.Log("ChartBoostWasViewed");
		DarkNebulaAds.ReportHaveSeenAd();
		DarkNebulaAds.PrepareChartBoostAds(levelNum);
		StartFadeIn(true);
	}

	private void VungleWasViewed()
	{
		DarkNebulaAds.Vungle.AdEnd -= VungleWasViewed;
		DarkNebulaAds.ReportHaveSeenAd();
		Debug.Log("VungleWasViewed");
		StartFadeIn(true);
	}

	private void RemoveAdsWasViewed()
	{
		Hud._removeAdsScreen.EventPlay -= RemoveAdsWasViewed;
		Hud._removeAdsScreen.gameObject.SetActive(false);
		Debug.Log("RemoveAdsWasViewed");
		StartFadeIn(true);
	}

	public void StartFadeIn(bool HasSeenAdThisTime = false)
	{
		// Start directly; no ad or purchase flow.
		UserSettings.I.SetHighestPlayedLevel(LevelNum);
		if (!_startedFadeIn)
		{
			_startedFadeIn = true;
			// Select this level's track before HUD resume can restore the menu.
			// A level without music must also stop the previous scene's track.
			PlayMusic();
			StartCoroutine(DoFadeIn());
		}
	}

	private IEnumerator DoFadeIn()
	{
		MeshRenderer[] list = UnityEngine.Object.FindObjectsOfType(typeof(MeshRenderer)) as MeshRenderer[];
		int tex = 0;
		MeshRenderer[] array = list;
		foreach (MeshRenderer item in array)
		{
			if (item != null && item.sharedMaterial != null && item.sharedMaterial.mainTexture != null)
			{
				int tw = item.sharedMaterial.mainTexture.width;
				tex++;
			}
		}
		Debug.Log("Accessed textures: " + tex);
		fade.SetScreenOverlayColor(new Color(0f, 0f, 0f, 1f));
		float t = Time.realtimeSinceStartup + 1f;
		while (t > Time.realtimeSinceStartup)
		{
			yield return 0;
		}
		hud.onResumeGame();
        GameHaptics.Play(HapticEvent.Spawn);
		skillMeter.Init();
		t = Time.realtimeSinceStartup;
		while (t > Time.realtimeSinceStartup)
		{
			yield return 0;
		}
		if (UserSettings.I.expert)
		{
			Invoke("ShowExpertMode", 1.33f);
		}
		if (levelNum != 1)
		{
			Invoke("PlayIntro", 1.15f);
		}
		fade.FadeComplete += OnFadeInComplete;
		fade.FadeIn(1f);
		ScriptManager.RunScript("intro", IsRestart);
	}

	public static void GameCompleted()
	{
		Credits.GoToEndOfGame = true;
		LoadLevel("Credits", 0.5f);
	}

	public static List<GameObject> FindEntitiesCloseToMaxDistance(Vector3 position, float shockWaveDistance)
	{
		List<GameObject> list = new List<GameObject>();
		RaycastHit[] array = Physics.CapsuleCastAll(position + new Vector3(0f, 512f + shockWaveDistance, 0f), position + new Vector3(0f, 250f, 0f), shockWaveDistance, new Vector3(0f, -512f + shockWaveDistance, 0f));
		RaycastHit[] array2 = array;
		foreach (RaycastHit raycastHit in array2)
		{
			list.Add(raycastHit.transform.gameObject);
		}
		return list;
	}

	public static Vector2 ToScreenCoords(Vector3 position)
	{
		return new Vector2(position.x - cameraFollow.windowPosX, position.z - cameraFollow.windowPosZ);
	}

	public static ParallaxLayer GetParallaxLayer(int index)
	{
		if (Layers == null) return null;
		foreach (ParallaxLayer layer in Layers)
		{
			if (layer != null && _world != null && layer.gameObject.scene == _world.gameObject.scene && layer.layerIndex == index)
			{
				return layer;
			}
		}
		return null;
	}

	private void Dispose()
	{
		if (_flagEndGame != null)
		{
			_flagEndGame.Callback -= GameCompleted;
			_flagEndGame.Dispose();
			_flagEndGame = null;
		}
		cameraFollow = null;
		visibleCamera = null;
		skillMeter = null;
		_world = null;
		vessel = null;
		hud = null;
	}

	public static bool EntityWithinScreen(Entity entity)
	{
		Vector3 vector = entity.transform.position;
		Vector3 vector2 = vector;
		if (entity.GetComponent<Collider>() != null)
		{
			vector = entity.GetComponent<Collider>().bounds.min;
			vector2 = entity.GetComponent<Collider>().bounds.max;
		}
        Camera visibleCamera=cameraFollow.GetComponent<Camera>();
        float halfHeight=visibleCamera.orthographicSize;
        float halfWidth=halfHeight*visibleCamera.aspect;
		if (cameraFollow.windowPosZ + halfHeight < vector.z)
		{
			return false;
		}
		if (cameraFollow.windowPosZ - halfHeight > vector2.z)
		{
			return false;
		}
		if (cameraFollow.windowPosX - halfWidth > vector2.x)
		{
			return false;
		}
		if (cameraFollow.windowPosX + halfWidth < vector.x)
		{
			return false;
		}
		return true;
	}

	public static void AddLayer(ParallaxLayer pLayer)
	{
		if (Layers == null)
		{
			Layers = new List<ParallaxLayer>();
		}
		Layers.RemoveAll(layer => layer == null);
		if (!Layers.Contains(pLayer)) Layers.Add(pLayer);
	}
	public static void RemoveLayer(ParallaxLayer layer) { if (Layers != null) Layers.Remove(layer); }

	private void CGFix()
	{
		object[] array = new object[1024];
		for (int i = 0; i < 1024; i++)
		{
			array[i] = new byte[1024];
		}
		array = null;
	}

	private void OnFadeInComplete()
	{
        SceneTransitionInput.Complete();
        ShardWallet.SpawnInLevel();
		fade.FadeComplete -= OnFadeInComplete;
		fade.enabled = false;
	}

	private static void PlayMusic()
	{
		Audio.ChangeMusicWithFade(_world.Music, 0.6f, 1f, true);
	}

	public static void PlayMusic(string name, int repeatCount = -1)
	{
		_world.Music = _world.GetMusicClip(name);
		if (repeatCount != -1)
		{
			Debug.LogWarning("PlayMusic with repeatCount " + repeatCount + " not implemented, looping forever...");
		}
		PlayMusic();
	}

	public AudioClip GetMusicClip(string name)
	{
		if (MusicClips == null)
		{
			return null;
		}
		AudioClip[] musicClips = MusicClips;
		foreach (AudioClip audioClip in musicClips)
		{
			if (audioClip != null && audioClip.name == name)
			{
				return audioClip;
			}
		}
		return null;
	}

	public static void ResumeMusic()
	{
		if (Audio.IsMusicMuted) return;
		if (_world.Music == null || !Audio.IsSameSoundPlaying("_music_", _world.Music)
			|| !Audio.ResumeMusic(1.5f, -1f))
		{
			PlayMusic();
		}
	}

	private void OnDestroy()
	{
		Dispose();
		_world = null;
	}

	public static void LoadLevel(string levelName, float fadeTime)
	{
        if (!SceneTransitionInput.TryBegin()) return;
		fade = _world.gameObject.AddComponent<CameraFade>();
		fade.SetScreenOverlayColor(new Color(0f, 0f, 0f, 0f));
		if (fade == null)
		{
			Debug.Log("World.LoadLevel Fade == null ");
			StubLoader.LoadLevel(levelName);
			return;
		}
		StubLoader.LevelToLoad = levelName;
		fade.enabled = true;
		fade.FadeComplete += DoLoadLevel;
		fade.FadeOut(fadeTime);
	}

	private static void DoLoadLevel()
	{
		if (_world != null)
		{
			CameraFade[] componentsInChildren = _world.GetComponentsInChildren<CameraFade>();
			CameraFade[] array = componentsInChildren;
			foreach (CameraFade cameraFade in array)
			{
				if (cameraFade.IsFading)
				{
					cameraFade.SetScreenOverlayColor(new Color(0f, 0f, 0f, 1f));
				}
			}
		}
		StubLoader.LoadLevel(StubLoader.LevelToLoad);
	}

	private void PlayIntro()
	{
		Audio.Play("intro", SfxIntro);
	}

	public static void RegisterLifeOrb()
	{
		lifeOrbsOnLevel++;
	}

	private void ShowExpertMode()
	{
		hud.ShowMessage(Language.Get("EXPERT MODE!"), true);
		Audio.Play("expertmode", SfxExpertMode, vessel.transform);
	}

	private void Update()
	{
		if (!IsRunning)
		{
			return;
		}
		ScriptManager.Update();
		if (Time.timeScale != 0f)
		{
			if (SignalUpdate != null)
			{
				SignalUpdate();
			}
			if (templeEffect != null)
			{
				templeEffect.Update();
			}
		}
	}

	public static bool IsAboutToShowOnScreen(float z, float height)
	{
		if (cameraFollow == null)
		{
			return false;
		}
		return z - height < cameraFollow.windowPosZ + Mathf.Max(480f, VisibleHalfSize.y) + 40f;
	}

	public static void SetDifficultyTo(float d)
	{
		float nextDifficulty = Mathf.Max(d, 0.8f);
		if (hasAnimationDifficulty)
		{
			animationTimeOffset += WorldTimeInSeconds * (_difficulty - nextDifficulty);
		}
		_difficulty = nextDifficulty;
		hasAnimationDifficulty = true;
		if (SignalDifficultyChanged != null)
		{
			SignalDifficultyChanged(d);
		}
	}

	public static void TriggerEndOfLevel()
	{
        GameHaptics.Play(HapticEvent.Finish);
		DarkNebulaMetrics.EndLevel(levelNum, UserSettings.I.expert);
		LevelCompleteScreen.Result.CompletionTime = Time.time - startTime;
		LevelCompleteScreen.Result.LevelNum = levelNum;
		LevelCompleteScreen.Result.OrbsOnLevel = lifeOrbsOnLevel;
		LevelCompleteScreen.Result.PlayerLostShields = vessel.ShieldsLostOnThisLevel;
		LevelCompleteScreen.Result.PlayerLivesLost = vessel.LivesLostOnThisLevel;
		LevelCompleteScreen.Result.OrbsCollected = vessel.OrbsCollectedOnThisLevel;
		LevelCompleteScreen.Result.Expert = UserSettings.I.expert;
		Audio.StopMusic(1.5f);
		LoadLevel("LevelComplete", 0.5f);
	}

	public static void TriggerFlag(int flag)
	{
		switch (flag)
		{
		case 0:
			return;
		case -1:
			TriggerEndOfLevel();
			break;
		}
		if (!Flags.ContainsKey(flag))
		{
			Flags[flag] = 0;
		}
		int num = (int)Flags[flag] + 1;
		Flags[flag] = num;
		if (num == 1)
		{
			ScriptManager.RunScriptNumber(flag);
		}
		Trigger(flag, num);
	}

	public static bool IsFrozen(float z)
	{
		if (z > cameraFollow.windowPosZ + Mathf.Max(480f, VisibleHalfSize.y) + 600f)
		{
			return true;
		}
		if (z < cameraFollow.windowPosZ - Mathf.Max(600f, VisibleHalfSize.y + 240f))
		{
			return true;
		}
		return false;
	}

	public static void TriggerVesselDied()
	{
		if (SignalVesselDied != null)
		{
			SignalVesselDied();
		}
	}

	public static void DisableAlwaysAliveAndKeepaliveEntities()
	{
		if (OnDisableAlwaysAliveAndKeepaliveEntities != null)
		{
			OnDisableAlwaysAliveAndKeepaliveEntities();
		}
	}

	public static void SetLayerEnabled(int pLayer, bool pEnabled)
	{
		ParallaxLayer parallaxLayer = GetParallaxLayer(pLayer);
		if (parallaxLayer != null)
		{
			parallaxLayer.Enabled = pEnabled;
		}
		else
		{
			Debug.LogWarning("Can't find layer " + pLayer);
		}
	}

	public bool IsTempleEffectActive()
	{
		return templeEffect.IsActive();
	}
}

using System.Collections;
using System.Collections.Generic;
using AssemblyCSharp;
using FLD;
using Palringo;
using UnityEngine;

public class World : MonoBehaviour
{
	public delegate void SignalUpdateHandler();

	public delegate void SignalDifficultyChangedHandler(float difficulty);

	public delegate void SignalFlagChangedHandler(int flag, int newValue);

	public const float FreezeMargin = 600f;

	public const float DefrostMargin = 500f;

	public static Vessel vessel;

	public static CameraFollow cameraFollow;

	public static SecureInt multipliedStartTime = 0;

	public static Hud hud;

	public static GameObject ParticlePrefab;

	public static GameObject ShockwavePrefab;

	public static SkillMeter skillMeter;

	public static bool isInMatrixMode = false;

	public static bool isEndOfMatrix = false;

	public static int levelNum = 1;

	public static int mapHeight = 1000;

	public static int OrbsOnLevel = 0;

	public int LevelNum = 1;

	public int MapHeight = 1000;

	public bool MatrixMode;

	public GameObject ShockwaveEntity;

	public GameObject ParticleEntity;

	public AudioClip SfxExpertMode;

	public AudioClip SfxIntro;

	public AudioClip Music;

	private static World _world;

	private static CameraFade fade;

	private static Hashtable Flags;

	public static float distortion = 0f;

	public static Shader distortionShader;

	public Shader DistortionShader;

	private static List<MonoBehaviour> _frozenObjects;

	private static List<SuperLifeOrb> _hcOrbs = new List<SuperLifeOrb>();

	private bool _startedFadeIn;
    private static Camera visibleCamera;
    public static float VisibleHalfHeight
    {
        get
        {
            if (visibleCamera == null && cameraFollow != null) visibleCamera = cameraFollow.GetComponent<Camera>();
            return visibleCamera != null ? visibleCamera.orthographicSize : Constants.VirtualScreenHeightHalf;
        }
    }

	public static float DefrostZ
	{
		get
		{
            return cameraFollow.transform.position.z + VisibleHalfHeight + 500f;
		}
	}

	public static float FreezeZ
	{
		get
		{
            return cameraFollow.transform.position.z + VisibleHalfHeight + 600f;
		}
	}

	public static float FreezeBackZ
	{
		get
		{
            return cameraFollow.transform.position.z - VisibleHalfHeight - 600f;
		}
	}

	public static bool IsBeginningOfLevel1
	{
		get
		{
			if (levelNum != 1)
			{
				return false;
			}
			return vessel.transform.position.z < 9400f;
		}
	}

	public static float SecondsPlayedLevel
	{
		get
		{
			return Time.time - (float)multipliedStartTime.Value / 100000f;
		}
	}

	public static event SignalUpdateHandler SignalUpdate;

	public static event SignalDifficultyChangedHandler SignalDifficultyChanged;

	public static event SignalFlagChangedHandler SignalFlagChanged;

	private void OnDisable()
	{
		vessel = null;
		cameraFollow = null;
        visibleCamera = null;
		hud = null;
		skillMeter = null;
		_world = null;
	}

	private void Awake()
	{
		_hcOrbs.Clear();
		_world = this;
		LevelManager.Current.LoadScenes();
		Time.timeScale = 0f;
		_frozenObjects = new List<MonoBehaviour>();
		fade = base.gameObject.GetComponent<CameraFade>();
		if (fade == null)
		{
			fade = base.gameObject.AddComponent<CameraFade>();
		}
		fade.SetScreenOverlayColor(Color.black);
		OrbsOnLevel = 0;
		ParticlePrefab = ParticleEntity;
		ShockwavePrefab = ShockwaveEntity;
		levelNum = LevelNum;
		mapHeight = MapHeight;
		isInMatrixMode = MatrixMode;
		distortionShader = DistortionShader;
		isEndOfMatrix = false;
		Flags = new Hashtable();
		skillMeter = new SkillMeter();
		vessel = GameObject.Find("VesselEntity").GetComponent<Vessel>();
		EnableBoosters();
		cameraFollow = GameObject.Find("Main Camera").GetComponent<CameraFollow>();
		hud = GameObject.Find("Hud").GetComponent<Hud>();
		multipliedStartTime = Mathf.RoundToInt(Time.time * 100000f);
	}

	private void EnableBoosters()
	{
		EnableSpeedBooster(BoosterManager.IsEnabled(Booster.GetIdForCurrentDifficulty(Booster.BoosterType.SPEED, levelNum)));
		EnableMagnetBooster(BoosterManager.IsEnabled(Booster.GetIdForCurrentDifficulty(Booster.BoosterType.MAGNET, levelNum)));
		EnableShieldBooster(BoosterManager.IsEnabled(Booster.GetIdForCurrentDifficulty(Booster.BoosterType.SHIELD, levelNum)));
	}

	private void EnableSpeedBooster(bool pEnable = true)
	{
		vessel.SpeedBooster = pEnable;
	}

	private void EnableMagnetBooster(bool pEnable = true)
	{
		vessel.MagnetBooster = pEnable;
	}

	private void EnableShieldBooster(bool pEnable = true)
	{
		vessel.ShieldBooster = pEnable;
	}

	private void DisableExtralifes()
	{
		Object[] array = Object.FindObjectsOfType(typeof(SuperLifeOrb));
		for (int i = 0; i < array.Length; i++)
		{
			SuperLifeOrb superLifeOrb = (SuperLifeOrb)array[i];
			superLifeOrb.gameObject.SetActive(false);
		}
	}

	private void SpawnHardCurrency()
	{
		int hCSpawnChance = UserProgress.Instance.HCSpawnChance;
		if (_hcOrbs.Count <= 0)
		{
			Debug.LogWarning("No hc orbs found on level, unable to spawn hard currency");
			return;
		}
		foreach (SuperLifeOrb hcOrb in _hcOrbs)
		{
			GameObject gameObject = hcOrb.gameObject;
			if (gameObject != null)
			{
				gameObject.SetActive(false);
			}
		}
		if (UserPurchases.Instance.HasUnlockAll)
		{
			return;
		}
		if (Random.Range(1, 101) < hCSpawnChance)
		{
			Debug.Log("One HC orb present this level, current spawn chance " + hCSpawnChance);
			int num = Random.Range(0, _hcOrbs.Count);
			for (int i = 0; i < _hcOrbs.Count; i++)
			{
				if (i == num)
				{
					_hcOrbs[i].gameObject.SetActive(true);
					break;
				}
			}
		}
		_hcOrbs.Clear();
	}

	public static void RegisterSuperLifeOrb(SuperLifeOrb pHCOrb)
	{
		_hcOrbs.Add(pHCOrb);
	}

	private void CGFix()
	{
		object[] array = new object[1024];
		for (int i = 0; i < 1024; i++)
		{
			array[i] = new byte[1024];
		}
		array = null;
	}

	private IEnumerator WaitForFadeInComplete()
	{
		float begin = Time.realtimeSinceStartup;
		while (begin + 0.5f + 0.5f > Time.realtimeSinceStartup)
		{
			if (begin > Time.realtimeSinceStartup)
			{
				begin = Time.realtimeSinceStartup;
			}
			yield return null;
		}
		StartFadeIn();
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
		if (!_startedFadeIn)
		{
			_startedFadeIn = true;
			PlayMusic();
			hud.onResumeGame();
        GameHaptics.Play(HapticEvent.Spawn);
			multipliedStartTime = Mathf.RoundToInt(Time.time * 100000f);
			skillMeter.Init();
			if (UserSettings.Instance.IsExpertMode)
			{
				Invoke("ShowExpertMode", 0.33f);
			}
			if (levelNum != 1)
			{
				Invoke("PlayIntro", 0.15f);
			}
			fade.FadeComplete += OnFadeInComplete;
			fade.FadeIn();
		}
	}

	private void OnFadeInComplete()
	{
        SceneTransitionInput.Complete();
		SpawnHardCurrency();
		fade.FadeComplete -= OnFadeInComplete;
		fade.enabled = false;
	}

	public static void PlayMusic()
	{
		Audio.PlayMusicWithCrossFade(_world.Music, 0.6f, 1f, true);
	}

	public static void ResumeMusic()
	{
		if (Audio.IsMusicMuted) return;
		if (!Audio.IsSameSoundPlaying("_music_", _world.Music) || !Audio.ResumeMusic(1.5f))
		{
			PlayMusic();
		}
	}

	public static void LoadLevel(string levelName, float fadeTime)
	{
        if (!SceneTransitionInput.TryBegin()) return;
		_hcOrbs.Clear();
		if (fade == null)
		{
			Debug.Log("World.LoadLevel Fade == null ");
			StubLoader.LevelToLoad = levelName;
			Application.LoadLevel("StubLoader");
		}
		else
		{
			StubLoader.LevelToLoad = levelName;
			fade.enabled = true;
			fade.FadeComplete += DoLoadLevel;
			fade.FadeOut(fadeTime);
		}
	}

	private static void DoLoadLevel()
	{
		distortion = 0f;
		Application.LoadLevel("StubLoader");
	}

	private void PlayIntro()
	{
		Audio.Play("intro", SfxIntro);
	}

	public static void RegisterLifeOrb()
	{
		OrbsOnLevel++;
		hud.UpdateOrbs();
	}

	private void ShowExpertMode()
	{
		hud.ShowMessage("EXPERT MODE!".Localize(), true);
		Audio.Play("expertmode", SfxExpertMode, vessel.transform);
	}

	private void Update()
	{
		if (SignalUpdate != null)
		{
			SignalUpdate();
		}
		for (int num = _frozenObjects.Count - 1; num >= 0; num--)
		{
			if (IsDefrosted(_frozenObjects[num].transform.position.z))
			{
				(_frozenObjects[num] as IFreezable).Defrost();
				_frozenObjects.RemoveAt(num);
			}
		}
		DrawDebugLines();
	}

	private void DrawDebugLines()
	{
		float x = -128f;
		float x2 = 384f;
		Debug.DrawLine(new Vector3(x, 0f, FreezeBackZ), new Vector3(x2, 0f, FreezeBackZ), Color.red);
		Debug.DrawLine(new Vector3(x, 0f, FreezeZ), new Vector3(x2, 0f, FreezeZ), Color.red);
		Debug.DrawLine(new Vector3(x, 0f, DefrostZ), new Vector3(x2, 0f, DefrostZ), Color.blue);
	}

	public static bool IsDefrosted(float z)
	{
		return z <= DefrostZ && z >= FreezeBackZ;
	}

	public static bool IsFrozen(float z)
	{
		return z > FreezeZ || z < FreezeBackZ;
	}

	public static void AddToFrozenObjects(MonoBehaviour go)
	{
		_frozenObjects.Add(go);
	}

	public static bool IsAboutToShowOnScreen(float z, float height)
	{
        return z - height < cameraFollow.transform.position.z + VisibleHalfHeight + 60f;
	}

	public static void SetDifficultyTo(float d)
	{
		if (SignalDifficultyChanged != null)
		{
			SignalDifficultyChanged(d);
		}
	}

	public static void TriggerEndOfLevel()
	{
        GameHaptics.Play(HapticEvent.Finish);
		if (!multipliedStartTime.IsConsistent)
		{
			Debug.Log("SecureInt warning: Start time has been hacked. Setting crappy score for player");
			LevelCompleteScreen.Result.CompletionTime = 9999f;
			DarkNebulaMetrics.LogHack("multipliedStartTime");
		}
		else
		{
			float completionTime = Time.time - (float)multipliedStartTime.Value / 100000f;
			LevelCompleteScreen.Result.CompletionTime = completionTime;
		}
		LevelCompleteScreen.Result.CompletionTime += (OrbsOnLevel - vessel.OrbsCollectedOnThisLevel).Value;
		LevelCompleteScreen.Result.LevelNum = levelNum;
		LevelCompleteScreen.Result.OrbsOnLevel = OrbsOnLevel;
		LevelCompleteScreen.Result.OrbsCollected = vessel.OrbsCollectedOnThisLevel.Value;
		LevelCompleteScreen.Result.Expert = UserSettings.Instance.IsExpertMode;
		Audio.StopMusic(1.5f);
		LoadLevel("LevelComplete", 0.5f);
	}

	public static void TriggerFlag(int flag)
	{
		if (!Flags.ContainsKey(flag))
		{
			Flags[flag] = 0;
		}
		int num = (int)Flags[flag] + 1;
		Flags[flag] = num;
		if (SignalFlagChanged != null)
		{
			SignalFlagChanged(flag, num);
		}
	}
}

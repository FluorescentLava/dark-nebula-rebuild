using System.Collections;
using FLD;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadLevelScreen : MonoBehaviour
{
	public GameObject title;

	public GameObject levelName;

	public SimpleText LoadingTip;

	public GameObject background;

	public GameObject helpGroup;

	public GameObject levelCircle;

	public GameObject levelCircleExtended;

	public GameObject instruction0;

	public GameObject instruction1;

	public GameObject instruction2;

	public GameObject instruction3;

	public GameObject bar0;

	public GameObject bar1;

	public GameObject bar2;

	public GameObject bar3;

	public GameObject bar4;

	public static int LevelToLoad = 16;

	private int highlightedIndex = 4;

	private CameraFade fade;

	private tk2dSprite[] bars = new tk2dSprite[5];

	private bool isShowingDialog;

	private float lastBarChange;

    private void Awake() { SceneTransitionInput.Hold(); }

	private void Start()
	{
        LevelMotionCalibration.Begin(q => UserSettings.I.centerCalibration = q);
		Debug.Log("LoadLevelScreen.Start() LevelToLoad:" + LevelToLoad);
		bars[0] = bar0.GetComponent<tk2dSprite>();
		bars[1] = bar1.GetComponent<tk2dSprite>();
		bars[2] = bar2.GetComponent<tk2dSprite>();
		bars[3] = bar3.GetComponent<tk2dSprite>();
		bars[4] = bar4.GetComponent<tk2dSprite>();
		Object.DontDestroyOnLoad(base.gameObject);
		Time.timeScale = 1f;
		UserSettings.I.SetLastSelectedLevel(LevelToLoad);
		PlayerPrefs.Save();
		DarkNebulaMetrics.StartLevelScreen(DarkNebulaMetrics.LevelScreens.LoadLevel_, LevelToLoad);
		title.GetComponentInChildren<SimpleText>().SetStringID("LOADING");
		levelName.GetComponentInChildren<SimpleText>().SetText(GetLevelName(LevelToLoad));
		if (UserSettings.I.GetPackFromLevelNum(LevelToLoad) == LevelPack.Original)
		{
			levelCircle.GetComponent<LevelCircle>().SelectLevel(LevelToLoad);
			levelCircleExtended.SetActive(false);
		}
		else
		{
			levelCircleExtended.GetComponent<LevelCircle>().SelectLevel(LevelToLoad);
			levelCircle.SetActive(false);
		}
		helpGroup.SetActive(false);
		if (LevelToLoad % 2 == 1 || (!BuildSettings.Current.ChartBoostEnabled && !BuildSettings.Current.VungleEnabled))
		{
			string text = Language.Get("YOU CAN RECALIBRATE");
			text = text + "\n" + Language.Get("AT ANY TIME DURING THE GAME");
			text = text + "\n" + Language.Get("BY PRESSING THE SCREEN");
			text = text + "\n" + Language.Get("WITH TWO FINGERS.");
			if (GamePreferences.Chinese) text = Language.Get("LOADING.HINT.CALIBRATE");
			LoadingTip.SetText(text);
		}
		else if (DarkNebulaStore.HasRemovedAds())
		{
			LoadingTip.SetStringID("LOADING HINT PAID");
		}
		else
		{
			LoadingTip.UseCurrentTextAsStringID();
		}
		if (Camera.main.aspect > 0.8f)
		{
			LoadingTip.transform.Translate(new Vector3(0f, 15f, 0f));
		}
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		Invoke("BeginLoad", 0.6f);
	}

	private void BeginLoad()
	{
		if (UserSettings.I.GetEnforceInternet())
		{
			DarkNebulaMetrics.HasInternet(false, LevelToLoad);
			if (!isShowingDialog)
			{
				isShowingDialog = true;
				uFuga.Instance.Delay(0.5f, ShowInternetConnectionDialog);
			}
		}
		else
		{
			DarkNebulaMetrics.HasInternet(true, LevelToLoad);
			StartCoroutine("Load");
		}
	}

	private void ShowInternetConnectionDialog()
	{
		DarkNebulaPopupManager.Instance.ShowMessageWithButton(Language.Get("no internet"), Language.Get("no internet body"), Language.Get("Try again"), (int x) =>
		{
			OnClickedTryAgain();
		});
	}

	private void OnClickedTryAgain()
	{
		isShowingDialog = false;
		BeginLoad();
	}

	private void Update()
	{
		if (lastBarChange + 1f < Time.realtimeSinceStartup)
		{
			int num = (highlightedIndex + 1) % 5;
			lastBarChange = Time.realtimeSinceStartup;
			bars[highlightedIndex].spriteId = bars[highlightedIndex].GetSpriteIdByName("UI_Loadingbar_Off");
			highlightedIndex = num;
			bars[highlightedIndex].spriteId = bars[highlightedIndex].GetSpriteIdByName("UI_Loadingbar_On");
		}
	}

	private IEnumerator Load()
	{
		string level = UserSettings.I.GetSceneName(LevelToLoad);
		Debug.Log("LoadLevelAsync " + level + "'");
		AsyncOperation loading = SceneManager.LoadSceneAsync(level, LoadSceneMode.Single);
        yield return loading;
        LevelMotionCalibration.EndSampling();
        foreach(AudioListener listener in GetComponentsInChildren<AudioListener>())listener.enabled=false;
		World.GamePreStart += OnGamePreStart;
	}

	private void OnGamePreStart()
	{
		Object.Destroy(base.gameObject);
	}

	private void OnDestroy()
	{
		World.GamePreStart -= OnGamePreStart;
	}

	public static string GetLevelName(int level)
	{
		string[] array = new string[20]
		{
			"ROUGH LANDING", "THE FACTORY", "CONTACT", "PLATFORMS", "SHARP EDGES", "SHOWDOWN", "PLEXUS GATEWAY", "DARK CORNERS", "NARROW PATHS", "STEPSTONES",
			"CRITTERS", "HEART OF DARKNESS", "SECOND GATEWAY", "BATTLEGROUND", "LEAPFROG", "SERPENTS", "SLINGSHOT", "DUST", "ENDGAME", "UNITY"
		};
		if (array.Length < level)
		{
			return level.ToString("D2") + " " + Language.Get("LEVEL " + level);
		}
		return level.ToString("D2") + " " + Language.Get(array[level - 1]);
	}
}

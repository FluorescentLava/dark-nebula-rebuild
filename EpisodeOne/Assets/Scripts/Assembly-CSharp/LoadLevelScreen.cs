using System;
using System.Collections;
using FLD;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadLevelScreen : MonoBehaviour
{
	public DynamicFont levelName;

	public GameObject helpGroup;

	public GameObject levelCircle;

	public GameObject levelCircleExtended;

	public GameObject bar0;

	public GameObject bar1;

	public GameObject bar2;

	public GameObject bar3;

	public GameObject bar4;

	private int highlightedIndex = 4;

	public static int LevelToLoad = 9;

	private CameraFade fade;

	private tk2dSprite[] bars = new tk2dSprite[5];

	private bool _showingAd;

	private float lastBarChange;

    private void Awake() { SceneTransitionInput.Hold(); }

	private void Start()
	{
		_showingAd = true;
		DarkNebulaAdsManager.Instance.ShowAd("LoadLevel").Wait((string obj) =>
		{
			_showingAd = false;
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
        LevelMotionCalibration.Begin(q => UserSettings.Instance.centerCalibration = q);
		bars[0] = bar0.GetComponent<tk2dSprite>();
		bars[1] = bar1.GetComponent<tk2dSprite>();
		bars[2] = bar2.GetComponent<tk2dSprite>();
		bars[3] = bar3.GetComponent<tk2dSprite>();
		bars[4] = bar4.GetComponent<tk2dSprite>();
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		Time.timeScale = 1f;
		GC.Collect();
		UserSettings.Instance.SetLastSelectedLevel(LevelToLoad, UserSettings.Instance.IsExpertMode);
		levelName.Text = GetLevelName(LevelToLoad);
        ConfigureLoadingText();
		if (LevelToLoad == 1)
		{
			helpGroup.SetActive(true);
			levelCircle.SetActive(false);
			levelCircleExtended.SetActive(false);
		}
		else
		{
			if (LevelToLoad <= 11)
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
		}
		PlayerPrefs.Save();
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		Invoke("BeginLoad", 0.6f);
	}

	private void BeginLoad()
	{
		StartCoroutine("Load");
	}

    private void ConfigureLoadingText()
    {
        // These bars were authored for Latin baselines. Center the visible ink
        // on their artwork and keep the Chinese sentence breaks intact.
        foreach (DynamicFont label in helpGroup.GetComponentsInChildren<DynamicFont>(true))
        {
            Renderer art = null;
            if (label.name == "InstructionsTitle") art = helpGroup.transform.Find("Thin Line").GetComponent<Renderer>();
            else if (label.name == "Instructions") art = helpGroup.transform.Find("Thick Line").GetComponent<Renderer>();
            else if (label.OriginalText == "COLLECT ALL HINT")
            {
                label.Anchor = TextAnchor.MiddleCenter;
                label.Alignment = TextAlignment.Center;
                label.SetOriginalPos(new Vector3(18f, 0f, label.transform.localPosition.z));
                art = label.transform.parent.Find("Button").GetComponent<Renderer>();
            }
            if (art == null) continue;
            label.Anchor = TextAnchor.MiddleCenter;
            if (GamePreferences.Chinese) label.Wrapping = DynamicFont.WrapType.NONE;
            LegacyControlTextLayout.Bind(label, art);
            label.Refresh();
        }
        foreach (DynamicFont label in new[] { levelName, transform.Find("LoadingText").GetComponent<DynamicFont>() })
        {
            label.SetOriginalPos();
            Vector3 position = label.transform.localPosition;
            position.y -= 5f;
            label.SetOriginalPos(position);
            label.Refresh();
        }
    }

	private void OnClickedTryAgain()
	{
		BeginLoad();
	}

	private void Update()
	{
		if (!_showingAd && lastBarChange + 1f < Time.realtimeSinceStartup)
		{
			int num = (highlightedIndex + 1) % 5;
			lastBarChange = Time.realtimeSinceStartup;
			bars[highlightedIndex].spriteId = bars[highlightedIndex].GetSpriteIdByName("ui_loading_bar/0");
			highlightedIndex = num;
			bars[highlightedIndex].spriteId = bars[highlightedIndex].GetSpriteIdByName("ui_loading_bar/1");
		}
	}

	private IEnumerator Load()
	{
		string level = LevelManager.Current.GetSceneName(LevelToLoad);
		AsyncOperation loading = SceneManager.LoadSceneAsync(level, LoadSceneMode.Single);
        yield return loading;
        LevelMotionCalibration.EndSampling();
        foreach(AudioListener listener in GetComponentsInChildren<AudioListener>())listener.enabled=false;
		fade.FadeOut();
		fade.FadeComplete += OnLoadComplete;
	}

	private void OnLoadComplete()
	{
		UnityEngine.Object.Destroy(base.gameObject);
		GameObject gameObject = GameObject.Find("!World");
		if (gameObject != null)
		{
			gameObject.GetComponent<World>().StartFadeIn();
			return;
		}
		Debug.LogError("No '!World' found. Try again in 1 sec.");
		Invoke("OnLoadComplete", 1f);
	}

	public static string GetLevelName(int level)
	{
		string[] array = new string[11]
		{
			"ASCENSION", "SPEED ZONE A", "LEAP OF FAITH", "THE GRINDHOUSE", "KINETICS", "SPEED ZONE B", "CROSSFIRE", "THE ABYSS", "NEMESIS", "THE MAINFRAME",
			"THE CORE"
		};
		if (level <= 0 || level > array.Length)
		{
			return level.ToString("D2") + " " + ("LEVEL " + level).Localize();
		}
		return level.ToString("D2") + " " + array[level - 1].Localize();
	}
}

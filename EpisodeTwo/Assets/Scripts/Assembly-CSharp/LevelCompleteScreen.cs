using System;
using FLD;
using UnityEngine;

public class LevelCompleteScreen : MonoBehaviour
{
	public static LevelCompleteResult Result;

	public static float PreviousBestCompletionTime = float.NaN;

	private static int[] parNormal = new int[32]
	{
		135, 165, 175, 180, 210, 200, 90, 140, 160, 170,
		200, 200, 90, 190, 190, 190, 225, 210, 420, 0,
		100, 80, 175, 50, 100, 110, 175, 50, 180, 130,
		175, 0
	};

	private static int[] parExpert = new int[32]
	{
		135, 165, 175, 180, 210, 200, 90, 140, 160, 170,
		200, 200, 90, 190, 190, 190, 225, 210, 420, 0,
		100, 85, 150, 38, 112, 110, 230, 38, 180, 140,
		180, 0
	};

	private StarValue StarOrb = StarValue.Unset;

	private StarValue StarTime = StarValue.Unset;

	private StarValue StarLife = StarValue.Unset;

	public GameObject PlayerGroup;

	public GameObject TimeGroup;

	public GameObject ResultGroup;

	public GameObject NameInput;

	public GameObject OrbsGroup;

	public GameObject ContinueButton;

	public GameObject GfxSuperStarScreen;

	public GameObject CameraObject;

	public AudioClip SfxTick;

	public AudioClip SfxStar;

	public AudioClip SfxStarBig;

	private CameraFade fade;

	private string userName;

	private TouchScreenKeyboard keyboard;

	private LevelCompleteSection[] sections;

	private int currentSection = -1;

	private Alert _alertScreen;

	private bool _done;
	private bool _initialized;
	private bool _skipping;
	private bool _continuing;

	private bool _gotBetterStarThenLast;

	private Vector3 OriginalPosition;

	private GameObject[] nameElements;
	private tk2dTextMesh[] nameLabels;
	private Color[] nameTopColors;
	private Color[] nameBottomColors;
	private tk2dBaseSprite nameBackground;
	private Color nameBackgroundColor;
	private Collider nameHitArea;
	private SimpleButton nameButton;
	private Vector3 nameTextScale;
	private float nameRevealStarted;
	private bool revealingName;

	private void Awake()
	{
		Time.timeScale = 1f;
		OriginalPosition = base.transform.position;
		SleepTimeoutManager.AllowSleepIn(60f);
		fade = CameraObject.AddComponent<CameraFade>();

		if (Result.LevelNum == 0)
		{
			Result.LevelNum = 1;
			Result.CompletionTime = 84.24f;
			Result.Star = StarValue.Gold;
			Result.Expert = false;
		}
		CalculateScore();
		DarkNebulaMetrics.StartLevelScreen(DarkNebulaMetrics.LevelScreens.LevelComplete_, Result.LevelNum);
		Setup();
		_initialized = true;
		fade.FadeComplete += OnFadeInComplete;
		fade.FadeIn();
	}

	private void Setup()
	{
		userName = UserSettings.I.GetUserName();
		sections = new LevelCompleteSection[4];
		sections[0] = OrbsGroup.GetComponent<LevelCompleteSection>();
		sections[0].StarResult = StarOrb;
		sections[1] = PlayerGroup.GetComponent<LevelCompleteSection>();
		sections[1].StarResult = StarLife;
		sections[2] = TimeGroup.GetComponent<LevelCompleteSection>();
		sections[2].StarResult = StarTime;
		sections[3] = ResultGroup.GetComponent<LevelCompleteSection>();
		sections[3].StarResult = Result.Star;
		sections[0].EventComplete += OnNextSection;
		sections[1].EventComplete += OnNextSection;
		sections[2].EventComplete += OnNextSection;
		sections[3].EventComplete += OnDone;
		sections[0].EventStar += OnPlayStarSound;
		sections[1].EventStar += OnPlayStarSound;
		sections[2].EventStar += OnPlayStarSound;
		sections[3].EventStar += OnPlayStarSound;
		sections[0].EventStartTick += OnStartTick;
		sections[2].EventStartTick += OnStartTick;
		NameInput.GetComponentInChildren<SimpleText>().SetLiteralText(userName);
		NameInput.GetComponent<SimpleButton>().ButtonUpEvent += OnChangeName;
		SetupNameReveal();
		ContinueButton.GetComponentInChildren<SimpleText>().SetStringID("CONTINUE");
		ContinueButton.GetComponent<SimpleButton>().ButtonUpEvent += OnContinue;
		_alertScreen = GetComponentInChildren<Alert>();
		SetEnabled();
	}

	private void SetEnabled()
	{
		ContinueButton.SetActive(false);
		_alertScreen.gameObject.SetActive(false);
		GfxSuperStarScreen.SetActive(false);
		foreach (GameObject element in nameElements) element.SetActive(false);
		nameButton.enabled = false;
		nameHitArea.enabled = false;
		revealingName = false;
	}

	private void SetupNameReveal()
	{
		LevelCompleteFinalSection finalSection = ResultGroup.GetComponent<LevelCompleteFinalSection>();
		nameElements = new[] { NameInput, finalSection.Title, finalSection.ClickToChange };
		nameLabels = new tk2dTextMesh[nameElements.Length];
		nameTopColors = new Color[nameElements.Length];
		nameBottomColors = new Color[nameElements.Length];
		for (int i = 0; i < nameElements.Length; i++)
		{
			nameLabels[i] = nameElements[i].GetComponentInChildren<tk2dTextMesh>(true);
			nameTopColors[i] = nameLabels[i].color;
			nameBottomColors[i] = nameLabels[i].color2;
		}
		nameBackground = NameInput.GetComponent<tk2dBaseSprite>();
		nameBackgroundColor = nameBackground.color;
		nameTextScale = nameLabels[0].scale;
		nameButton = NameInput.GetComponent<SimpleButton>();
		nameHitArea = NameInput.GetComponent<Collider>();
		if (nameHitArea == null)
		{
			// The field is hidden before SimpleButton.Start. Create its hit area
			// now so that Start cannot introduce an enabled collider mid-fade.
			BoxCollider hit = NameInput.AddComponent<BoxCollider>();
			Bounds bounds = nameBackground.GetBounds();
			hit.center = bounds.center;
			hit.size = new Vector3(bounds.size.x, bounds.size.y, .4f);
			nameHitArea = hit;
		}
	}

	private void RevealName(bool immediate = false)
	{
		foreach (GameObject element in nameElements) element.SetActive(true);
		// These renderers may have been hidden by the final section's Start.
		foreach (tk2dTextMesh label in nameLabels) label.GetComponent<Renderer>().enabled = true;
		nameBackground.GetComponent<Renderer>().enabled = true;
		FitNameText();
		revealingName = !immediate;
		nameRevealStarted = Time.unscaledTime;
		SetNameOpacity(immediate ? 1f : 0f);
	}

	private void SetNameOpacity(float opacity)
	{
		Color backgroundColor = nameBackgroundColor;
		backgroundColor.a *= opacity;
		nameBackground.color = backgroundColor;
		for (int i = 0; i < nameLabels.Length; i++)
		{
			Color top = nameTopColors[i], bottom = nameBottomColors[i];
			top.a *= opacity;
			bottom.a *= opacity;
			nameLabels[i].color = top;
			nameLabels[i].color2 = bottom;
			nameLabels[i].Commit();
		}
		nameHitArea.enabled = opacity >= 1f;
		nameButton.enabled = opacity >= 1f;
	}

	private void FitNameText()
	{
		tk2dTextMesh label = nameLabels[0];
		// Measure at its authored size, keeping the original left anchor and
		// inset. Long literal names can use wider glyphs than the bitmap font.
		label.scale = nameTextScale;
		label.Commit();
		Bounds art = nameBackground.GetComponent<Renderer>().bounds;
		float padding = 9f * Mathf.Abs(NameInput.transform.lossyScale.x);
		float available = art.max.x - label.transform.position.x - padding;
		float width = label.GetComponent<Renderer>().bounds.size.x;
		if (width > available && available > 0f)
		{
			label.scale = nameTextScale * (available / width);
			label.Commit();
		}
	}

	private void OnPlayStarSound(bool big)
	{
        GameHaptics.Play(HapticEvent.Star);
		Audio.Play("star" + UnityEngine.Random.value, (!big) ? SfxStar : SfxStarBig);
	}

	private void OnDisable()
	{
		CancelInvoke("OnStopTick");
		OnStopTick();
	}

	private void LateUpdate()
	{
		bool counting = _initialized && !_done && !_skipping && currentSection >= 0
			&& currentSection < sections.Length && sections[currentSection].IsCounting;
		if (counting) GameHaptics.Play(HapticEvent.Score);
		else GameHaptics.StopScore();
	}

	private void OnDestroy()
	{
		_initialized = false;
		if (sections != null)
		{
			for (int i = 0; i < sections.Length; i++)
			{
				if (sections[i] == null) continue;
				sections[i].EventComplete -= OnNextSection;
				sections[i].EventComplete -= OnDone;
				sections[i].EventStar -= OnPlayStarSound;
				sections[i].EventStartTick -= OnStartTick;
			}
		}
		if (fade != null)
		{
			fade.FadeComplete -= OnFadeInComplete;
			fade.FadeComplete -= OnFadeInSuperStar;
			fade.FadeComplete -= OnFadeOutSuperStar;
		}
		if (GfxSuperStarScreen != null)
			GfxSuperStarScreen.GetComponent<SuperStarScreen>().ScreenClosed -= OnSuperStarClose;
	}

	private void OnStartTick()
	{
		Audio.PlayLoop("Tick", SfxTick);
		Invoke("OnStopTick", 1.6666666f);
	}

	private void OnStopTick()
	{
		GameHaptics.StopScore();
		Audio.Stop("Tick", 0.1f);
	}

	private void OnFadeInComplete()
	{
		fade.FadeComplete -= OnFadeInComplete;
		OnNextSection();
	}

	private void OnNextSection()
	{
		if (!_initialized || _done || _skipping || currentSection + 1 >= sections.Length) return;
		currentSection++;
		if (currentSection == sections.Length - 1) RevealName();
		sections[currentSection].StartAnimation();
	}

	private void OnDone()
	{
		if (!_initialized || _done) return;
		_done = true;
		GameHaptics.StopScore();
		OnStopTick();
		RevealName(true);
		if (_gotBetterStarThenLast && UserSettings.I.GetIsAllGold(1, 19, Result.Expert))
		{
			Invoke("ShowSuperStar", 1f);
		}
		else
		{
			ContinueButton.SetActive(true);
		}
	}

	private void ShowSuperStar()
	{
		fade.FadeOut();
		fade.FadeComplete += OnFadeInSuperStar;
	}

	private void OnFadeInSuperStar()
	{
		base.gameObject.SetActive(false);
		GfxSuperStarScreen.SetActive(true);
		GfxSuperStarScreen.GetComponent<SuperStarScreen>().ScreenClosed += OnSuperStarClose;
		fade.FadeComplete -= OnFadeInSuperStar;
		fade.FadeIn();
	}

	private void OnSuperStarClose()
	{
		GfxSuperStarScreen.GetComponent<SuperStarScreen>().ScreenClosed -= OnSuperStarClose;
		fade.FadeComplete += OnFadeOutSuperStar;
		fade.FadeOut();
	}

	private void OnFadeOutSuperStar()
	{
		fade.FadeComplete -= OnFadeOutSuperStar;
		fade.FadeIn();
		base.gameObject.SetActive(true);
		SetEnabled();
		RevealName(true);
		ContinueButton.SetActive(true);
	}

	private void CalculateScore()
	{
		if (Result.LevelNum < 1 || Result.LevelNum > UserSettings.I.GetMaxLevel() || !LocalLeaderboard.IsValidTime(Result.CompletionTime)) return;
		StarOrb = StarValue.Bronze;
		if (Result.OrbsCollected >= Result.OrbsOnLevel - 5)
		{
			StarOrb = StarValue.Silver;
		}
		if (Result.OrbsCollected == Result.OrbsOnLevel)
		{
			StarOrb = StarValue.Gold;
		}
		int levelParTime = GetLevelParTime(Result.LevelNum, Result.Expert);
		StarTime = StarValue.Bronze;
		if (Result.CompletionTime < (float)(levelParTime + 20))
		{
			StarTime = StarValue.Silver;
		}
		if (Result.CompletionTime <= (float)levelParTime)
		{
			StarTime = StarValue.Gold;
		}
		if (Result.PlayerLivesLost > 0)
		{
			StarLife = StarValue.Bronze;
		}
		else if (Result.PlayerLostShields > 0)
		{
			StarLife = StarValue.Silver;
		}
		else
		{
			StarLife = StarValue.Gold;
		}
		Result.Star = StarOrb;
		if (StarTime < Result.Star)
		{
			Result.Star = StarTime;
		}
		if (StarLife < Result.Star)
		{
			Result.Star = StarLife;
		}
		PreviousBestCompletionTime = UserSettings.I.GetBestTime(Result.LevelNum, Result.Expert);
		if (PreviousBestCompletionTime == 0f)
		{
			DarkNebulaMetrics.LevelCompletedFirstTime(Result.LevelNum);
			PreviousBestCompletionTime = Result.CompletionTime;
		}
		if (PreviousBestCompletionTime >= Result.CompletionTime)
		{
			UserSettings.I.SetBestTime(Result.CompletionTime, Result.LevelNum, Result.Expert);
		}
		int levelStar = UserSettings.I.GetLevelStar(Result.LevelNum, Result.Expert);
		if (levelStar < (int)(Result.Star + 1))
		{
			_gotBetterStarThenLast = true;
			UserSettings.I.SetLevelStar(Result.LevelNum, (int)(Result.Star + 1), Result.Expert);
			DarkNebulaMetrics.NewHighscore(Result.LevelNum, Result.Star);
		}
		UserSettings.I.SetCompletedLevel(Result.LevelNum, Result.Expert);
	}

	public static int GetLevelParTime(int level, bool pExpert)
	{
		if (pExpert)
		{
			if (parExpert.Length < level)
			{
				return 150;
			}
			return parExpert[level - 1] + 1;
		}
		if (parNormal.Length < level)
		{
			return 150;
		}
		return parNormal[level - 1] + 1;
	}

	private void Update()
	{
		if (revealingName)
		{
			float opacity = Mathf.Clamp01((Time.unscaledTime - nameRevealStarted) / 0.3f);
			SetNameOpacity(opacity);
			revealingName = opacity < 1f;
		}
		if (keyboard != null)
		{
			userName = keyboard.text;
			if (keyboard.done)
			{
				SaveUserName();
				base.transform.position = OriginalPosition;
				keyboard = null;
			}
			NameInput.GetComponentInChildren<SimpleText>().SetLiteralText(userName);
			FitNameText();
		}
		if (_initialized && Input.GetMouseButton(0) && !_done)
		{
			_skipping = true;
			sections[0].SetComplete();
			sections[1].SetComplete();
			sections[2].SetComplete();
			sections[3].SetComplete();
			_skipping = false;
			OnDone();
		}
	}

	private void SaveUserName()
	{
		if (string.IsNullOrEmpty(userName))
		{
			userName = Language.Get("Player");
		}
		UserSettings.I.SetUserName(userName);
	}

	private void OnChangeName(SimpleButton button)
	{
		if (!_done || _continuing || revealingName || keyboard != null || !TouchScreenKeyboard.isSupported) return;
		keyboard = TouchScreenKeyboard.Open(userName, TouchScreenKeyboardType.Default);
		if (keyboard != null && TouchScreenKeyboard.area.height > 0f)
		{
			Debug.Log("TouchScreenKeyboard.area " + TouchScreenKeyboard.area);
			base.transform.position = OriginalPosition + new Vector3(0f, 100f, 0f);
		}
	}

	private void OnContinue(SimpleButton button)
	{
		if (!_initialized || !_done || _continuing) return;
		_continuing = true;
		SaveUserName();
		Invoke("OnGotoLevelCompleteLeaderboard", 0.6f);
		fade.FadeOut();
	}

	private void OnGotoLevelCompleteLeaderboard()
	{
		PostCompleteScreen.Result = Result;
		StubLoader.LoadLevel("PostComplete");
	}
}

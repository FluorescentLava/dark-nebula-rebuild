using System;
using System.Collections;
using FLD;
using UnityEngine;

public class MainMenuScreen : MonoBehaviour
{
	public GameObject btn0;

	public GameObject btn1;

	public GameObject btn2;

	public GameObject btn3;

	public GameObject btn4;

	public GameObject btnMoreGames;

	public GameObject smokeContainer;

	public AudioClip Music;

	public GameObject EtceteraAndroidManagerPrefab;

	private OptionScreen _optionScreen;

	private CalibrateScreen _calibrateScreen;

	private Alert _alertScreen;

	private CameraFade fade;

	private static bool _initialized;

	private static bool _introAttemptedThisSession;

	private bool _isEnabled = true;

	private bool _startingNewGame;

	private void Awake()
	{
		if (!_initialized)
		{
			_initialized = true;
			DarkNebulaMetrics.StartGame();
			DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.MainMenu);
			// Offline build: no billing, ads, remote promotion or call-home.
			if (btnMoreGames != null) btnMoreGames.SetActive(false);
		}
		else
		{
			DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.MainMenu);
		}
	}

	private void Start()
	{
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		btn0.GetComponentInChildren<SimpleText>().SetStringID("NEW GAME");
		btn0.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onNewGame;
		btn1.GetComponentInChildren<SimpleText>().SetStringID("CONTINUE GAME");
		btn1.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onContinueGame;
		btn2.GetComponentInChildren<SimpleText>().SetStringID("OPTIONS");
		btn2.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onOptions;
		btn3.GetComponentInChildren<SimpleText>().SetStringID("LEADERBOARDS");
		btn3.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onLeaderboards;
		btn4.GetComponentInChildren<SimpleText>().SetStringID("CREDITS");
		btn4.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onCredits;
		_optionScreen = GetComponentInChildren<OptionScreen>();
		_optionScreen.gameObject.SetActive(false);
		_optionScreen.EventGoBack += onCloseOptionScreen;
		_optionScreen.EventLaunchCalibrateControls += onLaunchCalibrate;
		_calibrateScreen = GetComponentInChildren<CalibrateScreen>();
		_calibrateScreen.gameObject.SetActive(false);
		_calibrateScreen.EventGoBack += onCloseCalibrateScreen;
		_alertScreen = GetComponentInChildren<Alert>();
		_alertScreen.gameObject.SetActive(false);
        var rows=gameObject.AddComponent<MenuRowLayout>();
        rows.Rows=new[]{btn0,btn1,btn2,btn3,btn4};
		if (!_introAttemptedThisSession && !UserSettings.I.GetHasVerifiedIntroMovieV1())
		{
			_introAttemptedThisSession = true;
			SetEnabled(false);
			StartCoroutine(PlayIntro());
		}
		else SetEnabled(true);
	}

	private void SetEnabled(bool on)
	{
		_isEnabled = on;
		GameObject gameObject = btn0.transform.parent.gameObject;
		SimpleButton[] componentsInChildren = gameObject.GetComponentsInChildren<SimpleButton>();
		foreach (SimpleButton simpleButton in componentsInChildren)
		{
			simpleButton.enabled = on;
		}
		bool hasProgress = UserSettings.I.GetHasAnyProgress();
		btn1.GetComponentInChildren<SimpleButton>().enabled = on && hasProgress;
		tk2dTextMesh continueLabel = btn1.GetComponentInChildren<tk2dTextMesh>();
		Color continueColor = continueLabel.color;
		continueColor.a = hasProgress ? 1f : 0.35f;
		continueLabel.color = continueColor;
		continueColor = continueLabel.color2;
		continueColor.a = hasProgress ? 1f : 0.35f;
		continueLabel.color2 = continueColor;
		continueLabel.Commit();
		if (on)
		{
			gameObject.transform.localPosition = Vector3.zero;
			Audio.ChangeMusicWithFade(Music, 0.6f, 1.5f, true);
			DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.MainMenu);
		}
		else
		{
			gameObject.transform.localPosition = Vector3.up * 1000f;
		}
		// A hidden menu must keep its offscreen position when the display changes.
		MenuContentFit layout = gameObject.GetComponent<MenuContentFit>();
		if (layout != null) layout.enabled = on;
	}

	private void Update()
	{
		if (_isEnabled && Input.GetKeyUp(KeyCode.Escape))
		{
			PromptQuit();
		}
	}

	private void onNewGame(SimpleButton source)
	{
		if (!_isEnabled || _startingNewGame) return;
		SetEnabled(false);
		if (UserSettings.I.GetHasAnyProgress())
		{
			Confirm("ARE YOU SURE?");
			_alertScreen.EventYES += OnEraseProgress;
			_alertScreen.SetBodyText(Language.Get("ALL PROGRESS") + "\n" + Language.Get("WILL BE LOST!"));
		}
		else
		{
			BeginNewGame(false);
		}
	}

	private void Confirm(string stringID)
	{
		SetEnabled(false);
		_alertScreen.gameObject.SetActive(true);
		_alertScreen.EventNO += onCloseConfirmScreen;
		_alertScreen.SetTitle(stringID);
	}

	private void PromptQuit()
	{
		Confirm("QUIT?");
		_alertScreen.EventYES += onQuit;
		_alertScreen.SetBodyText(string.Empty);
	}

	private void onQuit()
	{
		DarkNebulaMetrics.Quit();
		Audio.StopMusic(1.5f);
		fade.FadeOut();
		fade.FadeComplete += () =>
		{
			Debug.Log("Quit");
			Application.Quit();
		};
		SetEnabled(false);
	}

	private void onCloseConfirmScreen()
	{
		if (_startingNewGame) return;
		CloseConfirmation();
		SetEnabled(true);
	}

	private void CloseConfirmation()
	{
		_alertScreen.EventNO -= onCloseConfirmScreen;
		_alertScreen.EventYES -= OnEraseProgress;
		_alertScreen.EventYES -= onQuit;
		_alertScreen.gameObject.SetActive(false);
	}

	private void OnEraseProgress()
	{
		if (_startingNewGame || !_alertScreen.gameObject.activeSelf) return;
		BeginNewGame(true);
	}

	private void BeginNewGame(bool eraseProgress)
	{
		if (_startingNewGame) return;
		_startingNewGame = true;
		CloseConfirmation();
		SetEnabled(false);
		if (eraseProgress) EraseProgress();
		LoadLevelScreen.LevelToLoad = 1;
		World.IsRestart = false;
		StartCoroutine(LoadLevel("LoadLevel"));
	}

	private void EraseProgress()
	{
		for (int i = 1; i <= UserSettings.I.GetMaxLevel(); i++)
		{
			UserSettings.I.SetLevelStar(i, 0, false);
			UserSettings.I.SetLevelStar(i, 0, true);
			UserSettings.I.RemoveBestTime(i, false);
			UserSettings.I.RemoveBestTime(i, true);
		}
		UserSettings.I.ResetHighestPlayedLevel();
		UserSettings.I.SetLastSelectedLevel(UserSettings.I.GetPackMinLevel(LevelPack.Original));
		UserSettings.I.SetLastSelectedLevel(UserSettings.I.GetPackMinLevel(LevelPack.Extended));
		UserSettings.I.SetMaxSelectableLevel(UserSettings.I.GetPackMinLevel(LevelPack.Original));
		UserSettings.I.SetMaxSelectableLevel(UserSettings.I.GetPackMinLevel(LevelPack.Extended));
		LocalLeaderboard.ResetLeaderboards();
	}

	private void onContinueGame(SimpleButton source)
	{
		if (!_isEnabled || !UserSettings.I.GetHasAnyProgress()) return;
		StartCoroutine(LoadLevel("LevelSelect"));
	}

	private IEnumerator LoadLevel(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}

	private void onOptions(SimpleButton source)
	{
		fade.FadeOut();
		Invoke("onShowOption", 0.5f);
	}

	private void onShowOption()
	{
		SetEnabled(false);
		fade.FadeIn();
		_optionScreen.gameObject.SetActive(true);
	}

	private void onCloseOptionScreen()
	{
		fade.FadeOut();
		Invoke("onEnableMainMenu", 0.5f);
	}

	private void onEnableMainMenu()
	{
		_optionScreen.gameObject.SetActive(false);
		fade.FadeIn();
		SetEnabled(true);
	}

	private void onLaunchCalibrate()
	{
        _optionScreen.SetEnabled(false);
        SceneTransitionInput.SuppressFor(.7f);
		fade.FadeOut();
		Invoke("onShowCalibrate", 0.5f);
	}

	private void onShowCalibrate()
	{
		_optionScreen.SetEnabled(false);
		_optionScreen.gameObject.SetActive(false);
		fade.FadeIn();
		_calibrateScreen.gameObject.SetActive(true);
		DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.MainMenu_Calibrate);
	}

	private void onCloseCalibrateScreen()
	{
		fade.FadeOut();
		Invoke("onHideOption", 0.5f);
	}

	private void onHideOption()
	{
		_calibrateScreen.gameObject.SetActive(false);
		fade.FadeIn();
		_optionScreen.gameObject.SetActive(true);
		_optionScreen.SetEnabled(true);
	}

	private void onLeaderboards(SimpleButton source)
	{
		StartCoroutine(LoadLevel("Leaderboard"));
	}

	private void onCredits(SimpleButton source)
	{
		StartCoroutine(LoadLevel("Credits"));
	}

	private IEnumerator PlayIntro()
	{
		StoryMoviePlayer.Playback movie = StoryMoviePlayer.Play(this, StoryMoviePlayer.Intro);
		while (!movie.IsDone) yield return null;
		if (movie.WasHandled) UserSettings.I.SetHasVerifiedIntroMovieV1();
		// A skip must not also press a menu row or open the quit dialog.
		while (StoryMoviePlayer.SkipInputHeld) yield return null;
		yield return null;
		if (!isActiveAndEnabled) yield break;
		SetEnabled(true);
	}

	private void onEpisodeTwo(SimpleButton source)
	{
		StartCoroutine(LoadLevel("EpisodeTwo"));
	}
}

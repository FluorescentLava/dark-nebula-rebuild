using System.Collections;
using FLD;
using UnityEngine;

public class MainMenuScreen : MonoBehaviour
{
	private static bool _initialized;

	[SerializeField]
	private SimpleButton _playGameButton;

	[SerializeField]
	private SimpleButton _optionsButton;

	[SerializeField]
	private SimpleButton _leaderboardsButton;

	[SerializeField]
	private SimpleButton _quitButton;

	[SerializeField]
	private SimpleButton _cheatButton;

	[SerializeField]
	private CheatScreen _cheatScreen;

	public GetPalringoSimpleButton palringoButton;

	public OpenPalringoSimpleButton openPalringoButton;

	public AudioClip Music;

	public Transform Container;

	private OptionScreen _optionScreen;

	private CalibrateScreen _calibrateScreen;

	private Alert _alertScreen;

	private CameraFade fade;

	private bool _isEnabled = true;

	private void Awake()
	{
		if (!_initialized)
		{
			_initialized = true;
			UserProgress.Instance.Init();
			UserPurchases.Instance.Init();
			LevelManager.Current.LoadScenes();
			IAPmanager.Instance.InitIAPService();
			DarkNebulaAdsManager.Instance.InitInterstitialAdsService();
			BoosterManager.AwardStartupBoosters();
		}
	}

	private void Start()
	{
		foreach (DynamicFont label in GetComponentsInChildren<DynamicFont>(true))
			if (label.OriginalText == "SUBTITLE") label.SetLiteralText("Episode One");
		DarkNebulaAdsManager.Instance.ShowAd("MainMenu").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		Screen.sleepTimeout = -1;
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_playGameButton.ButtonUpEvent += onPlayGame;
		_optionsButton.ButtonUpEvent += onOptions;
		_leaderboardsButton.gameObject.SetActive(true);
        _leaderboardsButton.ButtonUpEvent += onLeaderboards;
        SimpleButton newGame=Object.Instantiate(_playGameButton,_playGameButton.transform.parent);
        newGame.name="New Game";newGame.Text="NEW GAME";
        newGame.targetObject=null;newGame.messageName=string.Empty;
        newGame.ButtonUpEvent += source => {LoadLevelScreen.LevelToLoad=1;StartCoroutine(LoadLevel("BoosterSelection"));};
		_playGameButton.GetComponentInChildren<DynamicFont>().Text = "CONTINUE GAME".Localize();
		_optionsButton.GetComponentInChildren<DynamicFont>().Text = "OPTIONS".Localize();
		_leaderboardsButton.GetComponentInChildren<DynamicFont>().Text = "LEADERBOARDS".Localize();
		_quitButton.GetComponentInChildren<DynamicFont>().Text = "QUIT".Localize();
		_optionScreen = GetComponentInChildren<OptionScreen>();
		_optionScreen.gameObject.SetActive(false);
		_optionScreen.EventGoBack += onCloseOptionScreen;
		_optionScreen.EventLaunchCalibrateControls += onLaunchCalibrate;
		_calibrateScreen = GetComponentInChildren<CalibrateScreen>();
		_calibrateScreen.gameObject.SetActive(false);
		_calibrateScreen.EventGoBack += onCloseCalibrateScreen;
        _quitButton.gameObject.SetActive(true);
        _quitButton.Text="EPISODE TWO";
        _quitButton.SetSpriteNames("ep2_button/1","ep2_button/0","ep2_button/0");
        _quitButton.ButtonUpEvent+=onEpisodeTwo;
        SimpleButton achievements = Object.Instantiate(_leaderboardsButton, _leaderboardsButton.transform.parent);
        achievements.name = "Achievements";
        achievements.Text = "ACHIEVEMENTS";
        achievements.targetObject = null;
        achievements.messageName = string.Empty;
        achievements.ButtonUpEvent += onAchievements;
        var rows=gameObject.AddComponent<MenuRowLayout>();
        rows.Rows=new[]{newGame.gameObject,_playGameButton.gameObject,_optionsButton.gameObject,_leaderboardsButton.gameObject,achievements.gameObject,_quitButton.gameObject};

		_alertScreen = GetComponentInChildren<Alert>();
		_alertScreen.gameObject.SetActive(false);
		PlayMenuMusic();
		UpdatePalringoButton();
		palringoButton.ButtonUpdated = UpdateOpenPalringoButton;
	}

	public void PlayMenuMusic()
	{
		Audio.PlayMusicWithCrossFade(Music, 0.6f, 1.5f, true);
	}

	private void UpdatePalringoButton()
	{
		palringoButton.UpdateStatus();
		openPalringoButton.UpdateStatus();
	}

	private void UpdateOpenPalringoButton()
	{
		openPalringoButton.UpdateStatus();
	}

	private void OnQuitButton(SimpleButton source)
	{
		onQuit();
	}

	private void SetEnabled(bool on)
	{
		_isEnabled = on;
		SimpleButton[] componentsInChildren = _playGameButton.transform.parent.gameObject.GetComponentsInChildren<SimpleButton>();
		foreach (SimpleButton simpleButton in componentsInChildren)
		{
			simpleButton.enabled = on;
		}
		// Hiding by translating the menu can leave its lower rows visible on
		// tall displays. This container holds only the main-menu presentation.
		Container.localPosition=Vector3.zero;
		Container.gameObject.SetActive(on);
	}

	private void Update()
	{
		if (_isEnabled && Input.GetKeyUp(KeyCode.Escape))
		{
			PromptQuit();
		}
	}

	private void onPlayGame(SimpleButton source)
	{
		if (!UserProgress.Instance.GetHasAnyProgress())
		{
			LoadLevelScreen.LevelToLoad = 1;
			StartCoroutine(LoadLevel("BoosterSelection"));
		}
		else
		{
			StartCoroutine(LoadLevel("LevelSelect"));
		}
	}

	private void onAchievements(SimpleButton source)
	{
		AchievementsScreen.Open(_optionScreen, () => SetEnabled(true));
		SetEnabled(false);
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
		SetEnabled(true);
		_alertScreen.EventNO -= onCloseConfirmScreen;
		_alertScreen.EventYES -= onQuit;
		_alertScreen.EventYES -= onCloseConfirmScreen;
		_alertScreen.gameObject.SetActive(false);
	}

	private IEnumerator LoadLevel(string levelName)
	{
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
		UpdatePalringoButton();
	}

	private void onLaunchCalibrate()
	{
		fade.FadeOut();
		Invoke("onShowCalibrate", 0.5f);
	}

	private void onShowCalibrate()
	{
		_optionScreen.gameObject.SetActive(false);
		fade.FadeIn();
		_calibrateScreen.gameObject.SetActive(true);
		_calibrateScreen.Show();
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
	}

	private void onLeaderboards(SimpleButton source)
	{
		StartCoroutine(LoadLevel("Leaderboard"));
	}

	private void onEpisodeTwo(SimpleButton source)
	{
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
            using(var intent=new AndroidJavaObject("android.content.Intent","android.intent.action.MAIN"))
            {
                intent.Call<AndroidJavaObject>("setClassName","com.darknebula.reference.tall","com.unity3d.player.UnityPlayerActivity");
                intent.Call<AndroidJavaObject>("addFlags",0x10000000);
                activity.Call("startActivity",intent);
            }
        }
        catch(System.Exception)
        {
            Confirm("EPISODE TWO");
            _alertScreen.SetTitleText("EPISODE TWO");
            _alertScreen.SetBodyText("INSTALL EPISODE TWO TO OPEN IT.");
            _alertScreen.EventYES += onCloseConfirmScreen;
        }
#else
        Confirm("EPISODE TWO");
        _alertScreen.SetTitleText("EPISODE TWO");
        _alertScreen.SetBodyText("OPEN THE EPISODE TWO PROJECT.");
        _alertScreen.EventYES += onCloseConfirmScreen;
#endif
	}
}

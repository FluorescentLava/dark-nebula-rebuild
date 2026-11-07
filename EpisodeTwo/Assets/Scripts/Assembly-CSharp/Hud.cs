using FLD;
using UnityEngine;

public class Hud : MonoBehaviour
{
	private enum State
	{
		Playing = 1,
		Pause = 2
	}

	private const float right = 0.85f;

	private const float left = 0.06f;

	private const float top = 0.955f;

	public GameObject GfxTextLives;

	public GameObject GfxTextOrbs;

	public GameObject GfxIconLifeOrb;

	public GameObject GfxIconLives;

	public ImageActionBehaviour ImageScreen;

	private tk2dTextMesh _textLives;
    private tk2dTextMesh _textTime;
    private GameObject GfxTextTime;
    private bool timerHudVisible = true;

	private tk2dTextMesh _textOrbs;

	private float lifeOrbScale = 1f;

	private State state = State.Playing;

	private TextMessage _message;

	private ShieldMessage _shieldMessage;

	private ShieldMessageDown _shieldMessageDown;

	public static PauseScreen _pauseScreen;

	public IngameOptionScreen _optionScreen;

	public CalibrateScreen _calibrateScreen;

	public ConfirmScreen confirmScreen;

	private static ConfirmScreen _confirmScreen;

	private static GameOverScreen _gameOverScreen;

	public static RemoveAdsScreen _removeAdsScreen;

	public static Hud Instance;

	private Vector3 orbDiff;

	private Vector3 lifeDiff;

	private bool _hasSampledHudDiff;

	private float _screenShrinkPercent;

	private float hasCalibratedThisTouch;

	private float _lastCalibration;

	public static bool ImageActionRunning;

	private void OnDestroy()
	{
		_textLives = null;
		_textOrbs = null;
		GfxTextLives = null;
		GfxTextOrbs = null;
		GfxIconLifeOrb = null;
		GfxIconLives = null;
		_message = null;
	}

	private void SampleHudDiff()
	{
		if (!_hasSampledHudDiff)
		{
			_hasSampledHudDiff = true;
			orbDiff = GfxIconLifeOrb.transform.localPosition - GfxTextOrbs.transform.localPosition;
			lifeDiff = GfxIconLives.transform.localPosition - GfxTextLives.transform.localPosition;
		}
	}

	private void Awake()
	{
		Instance = this;
        GfxTextTime = Instantiate(GfxTextLives, GfxTextLives.transform.parent);
        GfxTextTime.name = "LevelTimer";
        _textTime = GfxTextTime.GetComponent<tk2dTextMesh>();
        _textTime.anchor = TextAnchor.UpperCenter;
        _textTime.maxChars = 16;
        _textTime.text = "00:00.000";
        _textTime.Commit();
		_textLives = GfxTextLives.GetComponent<tk2dTextMesh>();
		_textOrbs = GfxTextOrbs.GetComponent<tk2dTextMesh>();
		_pauseScreen = GetComponentInChildren<PauseScreen>();
		_pauseScreen.gameObject.SetActive(false);
		_pauseScreen.EventResumeGame += onResumeGame;
		_pauseScreen.EventLaunchOptions += onLaunchOptions;
		_optionScreen.SpheroConnectScreen.gameObject.SetActive(false);
		_optionScreen.gameObject.SetActive(false);
		_optionScreen.EventGoBack += onCloseIngameOptions;
		_optionScreen.EventLaunchCalibrateControls += onLaunchCalibrateScreen;
		_calibrateScreen.gameObject.SetActive(false);
		_calibrateScreen.EventGoBack += onCloseCalibrateScreen;
		_confirmScreen = confirmScreen;
		_confirmScreen.gameObject.SetActive(false);
		_removeAdsScreen = GetComponentInChildren<RemoveAdsScreen>();
		_removeAdsScreen.gameObject.SetActive(false);
		_gameOverScreen = GetComponentInChildren<GameOverScreen>();
		_gameOverScreen.gameObject.SetActive(false);
		_gameOverScreen.EventResume += onResumeGame;
		_gameOverScreen.EventMainMenu += () =>
		{
			World.LoadLevel("MainMenu", 0.5f);
		};
		_gameOverScreen.EventContinue += () =>
		{
			Debug.Log("onRestartGame Level" + World.levelNum);
			LoadLevelScreen.LevelToLoad = World.levelNum;
			World.LoadLevel("LoadLevel", 0.5f);
		};
		_pauseScreen.confirmScreen = _confirmScreen;
		UpdateLives();
		UpdateOrbs();
		if (World.isInMatrixMode)
		{
			GfxIconLifeOrb.GetComponent<Renderer>().enabled = false;
			GfxIconLives.SetActive(false);
			GfxTextLives.GetComponent<Renderer>().enabled = false;
			GfxTextOrbs.GetComponent<Renderer>().enabled = false;
		}
		FixAspect();
	}

	public static void disablePauseMenu()
	{
		if (_pauseScreen != null)
		{
			_pauseScreen.gameObject.SetActive(false);
		}
	}

	private void OnApplicationPause(bool pPaused)
	{
		if (pPaused && state != State.Pause)
		{
			Pause();
		}
	}

	private void OnApplicationFocus(bool pFocus)
	{
		Debug.Log("OnApplicationFocus " + pFocus);
		if (!pFocus && state != State.Pause)
		{
			Pause();
		}
	}

	private void FixAspect()
	{
		Camera camera=GetComponentInChildren<Camera>();
		SampleHudDiff();
		UnifiedHudLayout.Apply(camera,GfxIconLifeOrb,GfxTextOrbs,GfxIconLives,GfxTextLives,GfxTextTime);
	}

    private Rect _lastLayoutSafe;
    private int _layoutWidth, _layoutHeight;
    private float _layoutOrtho=-1f,_layoutAspect=-1f;
    private void LateUpdate()
    {
        Camera camera=GetComponentInChildren<Camera>();
        if(camera==null)return;
        Rect safe=LegacyScreenFit.ContentRect();
        if(safe!=_lastLayoutSafe || Screen.width!=_layoutWidth || Screen.height!=_layoutHeight || !Mathf.Approximately(_layoutOrtho,camera.orthographicSize) || !Mathf.Approximately(_layoutAspect,camera.aspect))
        {
            _lastLayoutSafe=safe;_layoutWidth=Screen.width;_layoutHeight=Screen.height;
            _layoutOrtho=camera.orthographicSize;_layoutAspect=camera.aspect;
            FixAspect();
        }
    }

	public void ShowHudIcons(bool show = true)
	{
        timerHudVisible = show;
        _textTime.GetComponent<Renderer>().enabled = show && GamePreferences.ShowTimer;
		GfxIconLifeOrb.GetComponent<Renderer>().enabled = show;
		GfxIconLives.SetActive(show);
		GfxTextLives.GetComponent<Renderer>().enabled = show;
		GfxTextOrbs.GetComponent<Renderer>().enabled = show;
		FixAspect();
	}

	private void onLaunchCalibrateScreen()
	{
		_optionScreen.SetEnabled(false);
		_optionScreen.gameObject.SetActive(false);
		_calibrateScreen.gameObject.SetActive(true);
		DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.Pause_Calibrate);
	}

	private void onCloseCalibrateScreen()
	{
		_calibrateScreen.gameObject.SetActive(false);
		_optionScreen.gameObject.SetActive(true);
		_optionScreen.SetEnabled(true);
	}

	private void onLaunchOptions()
	{
		_pauseScreen.gameObject.SetActive(false);
		_optionScreen.gameObject.SetActive(true);
	}

	private void onCloseIngameOptions()
	{
		_optionScreen.gameObject.SetActive(false);
		_pauseScreen.gameObject.SetActive(true);
		_pauseScreen.SetEnabled(true);
	}

	private void Update()
    {
        if (_textTime != null)
        {
            _textTime.GetComponent<Renderer>().enabled = timerHudVisible && GamePreferences.ShowTimer && !World.isInMatrixMode;
            _textTime.text = LevelCompleteTimeSection.ToTimeString(World.WorldTimeInSeconds);
            _textTime.Commit();
        }
		if (state == State.Playing)
		{
			if (Input.touchCount > 1)
			{
				if (_lastCalibration + 4f < Time.time)
				{
					_lastCalibration = Time.time;
					World.hud.ShowMessage(Language.Get("CALIBRATED!"));
					UserSettings.I.Calibrate();
				}
				hasCalibratedThisTouch = Time.realtimeSinceStartup;
			}
			else if (Input.GetMouseButtonUp(0) && hasCalibratedThisTouch < Time.realtimeSinceStartup - 1f && !ImageActionRunning && IsMouseWithinMargin(40f))
			{
				Pause();
			}
			if (Input.GetKeyUp(KeyCode.Menu) || Input.GetKeyUp(KeyCode.Escape))
			{
				Pause();
			}
			if (lifeOrbScale > 1f)
			{
				lifeOrbScale -= 0.075f * Time.deltaTime * 30f;
			}
			else
			{
				lifeOrbScale = 1f;
			}
			GfxIconLifeOrb.transform.localScale = Vector3.one * lifeOrbScale;
		}
		if (Vessel.Lives > 1)
		{
			GfxIconLives.GetComponent<Renderer>().enabled = true;
		}
		else if (Mathf.FloorToInt(Time.realtimeSinceStartup * 2f) % 2 == 0)
		{
			GfxIconLives.GetComponent<Renderer>().enabled = true;
		}
		else
		{
			GfxIconLives.GetComponent<Renderer>().enabled = false;
		}
	}

	private bool IsMouseWithinMargin(float margin)
	{
		Camera camera = GetComponentInChildren<Camera>();
		if (camera == null) return false;
		Vector3 point = camera.ScreenToViewportPoint(Input.mousePosition);
		if (point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f) return false;
		float num = point.x * 320f;
		float num2 = point.y * 480f;
		if (num < margin)
		{
			return false;
		}
		if (num2 < margin)
		{
			return false;
		}
		if (num2 > 480f - margin)
		{
			return false;
		}
		if (num > 320f - margin)
		{
			return false;
		}
		return true;
	}

	public void Pause()
	{
		Pause(true);
	}

	private void Pause(bool cancelHaptics)
	{
        if (cancelHaptics && (SceneTransitionInput.Blocking || ImageActionRunning)) return;
		if (cancelHaptics) GameHaptics.Cancel();
		Time.timeScale = 0f;
		state = State.Pause;
		if (!(_pauseScreen == null))
		{
			DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.Pause);
			_pauseScreen.gameObject.SetActive(true);
			Audio.PauseMusic(1.5f);
			SleepTimeoutManager.AllowSleepIn(60f);
		}
	}

	public void onResumeGame()
	{
		_pauseScreen.gameObject.SetActive(false);
		state = State.Playing;
		Time.timeScale = 1f;
		World.ResumeMusic();
		DarkNebulaMetrics.StartLevelScreen(DarkNebulaMetrics.LevelScreens.Level_, World.levelNum);
		SleepTimeoutManager.NeverSleep();
	}

	public void AddOrb()
	{
		lifeOrbScale = 2.2f;
		UpdateOrbs();
	}

	public void UpdateLives()
	{
		if (!(_textLives == null))
		{
			int num = Vessel.Lives - 1;
			if (num < 0)
			{
				num = 0;
			}
			_textLives.text = "X" + num;
			_textLives.Commit();
		}
	}

	public void UpdateOrbs()
	{
		if (!(_textOrbs == null))
		{
			_textOrbs.text = Vessel.Orbs + "/100";
			_textOrbs.Commit();
		}
	}

	public void ShowMessage(string message, bool red = false)
	{
		destroyMessage();
		_message = ObjectPool.instance.GetObjectForType<TextMessage>();
		_message.transform.parent = base.transform;
		_message.transform.localScale = Vector3.one * (1f - _screenShrinkPercent * 0.3f);
		_message.SetMessage(message.ToUpper(), red);
	}

	public void Reset()
	{
		destroyShieldMessage();
	}

	private void destroyMessage()
	{
		if (!(_message == null))
		{
			TextMessage message = _message;
			_message = null;
			message.StopMessage();
			ObjectPool.instance.PoolObject(message);
		}
	}

	public void FinishMessage(TextMessage message)
	{
		if (_message == message) destroyMessage();
	}

	private void destroyShieldMessage()
	{
		if (_shieldMessageDown != null)
		{
			ObjectPool.instance.PoolObject(_shieldMessageDown);
			_shieldMessageDown = null;
		}
		if (_shieldMessage != null)
		{
			ObjectPool.instance.PoolObject(_shieldMessage);
			_shieldMessage = null;
		}
	}

	public void ShowShieldDownMessage()
	{
		destroyShieldMessage();
		_shieldMessageDown = ObjectPool.instance.GetObjectForType<ShieldMessageDown>();
		_shieldMessageDown.transform.parent = base.transform;
		_shieldMessageDown.transform.localPosition = new Vector3(0f, -140f, 0f);
		_shieldMessageDown.SetStringID("SHIELD DOWN");
	}

	public void ShowShieldRestored()
	{
		ShowShieldUpMessage("SHIELD RESTORED");
	}

	public void ShowShieldFound()
	{
		ShowShieldUpMessage("SHIELD FOUND");
	}

	private void ShowShieldUpMessage(string stringID)
	{
		destroyShieldMessage();
		_shieldMessage = ObjectPool.instance.GetObjectForType<ShieldMessage>();
		_shieldMessage.transform.parent = base.transform;
		_shieldMessage.transform.localPosition = new Vector3(0f, -140f, 0f);
		_shieldMessage.SetStringID(stringID);
		_shieldMessage.Play();
	}

	public static void ShowGameOverScreen()
	{
		// The game-over transition must let the death feedback finish.
		Instance.Pause(false);
		_pauseScreen.gameObject.SetActive(false);
		_gameOverScreen.Show();
	}
}

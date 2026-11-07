using UnityEngine;

public class PauseScreen : SimpleScreen
{
	public GameObject ConfirmScreenPrefab;

	public ConfirmScreen confirmScreen;

	private bool _isEnabled = true;
	private int _inputEnabledFrame = -1;

	public event HandlerDelegate EventResumeGame;

	public event HandlerDelegate EventLaunchOptions;

	private void Start()
	{
		lblTitle.GetComponentInChildren<SimpleText>().SetStringID("GAME PAUSED");
		btn0.GetComponentInChildren<SimpleText>().SetStringID("RESUME GAME");
		btn0.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			EventResumeGame();
		};
		btn1.GetComponentInChildren<SimpleText>().SetStringID("OPTIONS");
		btn1.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			SetEnabled(false);
			if (EventLaunchOptions != null)
			{
				EventLaunchOptions();
			}
		};
		btn2.GetComponentInChildren<SimpleText>().SetStringID("RESTART LEVEL");
		btn2.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			Confirm("RESTART LEVEL?");
			confirmScreen.EventYES += onRestartGame;
		};
		btn3.GetComponentInChildren<SimpleText>().SetStringID("MAIN MENU");
		btn3.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			ShowMainMenuPrompt();
		};
	}

	private void ShowMainMenuPrompt()
	{
		Confirm("EXIT TO MAIN MENU?");
		confirmScreen.EventYES += onMainMenu;
	}

	private void OnEnable()
	{
		_inputEnabledFrame = Time.frameCount;
	}

	private void Update()
	{
		if (_isEnabled && Time.frameCount > _inputEnabledFrame)
		{
			if (Input.GetKeyUp(KeyCode.Escape))
			{
				EventResumeGame();
			}
			if (Input.GetKeyUp(KeyCode.Menu))
			{
				ShowMainMenuPrompt();
			}
		}
	}

	public void SetEnabled(bool on)
	{
		_isEnabled = on;
		if (on) _inputEnabledFrame = Time.frameCount;
		if (on)
		{
			DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.Pause);
		}
		btn0.GetComponentInChildren<SimpleButton>().enabled = on;
		btn1.GetComponentInChildren<SimpleButton>().enabled = on;
		btn2.GetComponentInChildren<SimpleButton>().enabled = on;
		btn3.GetComponentInChildren<SimpleButton>().enabled = on;
	}

	private void Confirm(string stringID)
	{
		SetEnabled(false);
		gameObject.SetActive(false);
		confirmScreen.gameObject.SetActive(true);
		confirmScreen.EventNO += onCloseConfirmScreen;
		confirmScreen.SetTitle(stringID);
	}

	private void onCloseConfirmScreen()
	{
		gameObject.SetActive(true);
		SetEnabled(true);
		confirmScreen.EventNO -= onCloseConfirmScreen;
		confirmScreen.EventYES -= onMainMenu;
		confirmScreen.EventYES -= onRestartGame;
		confirmScreen.gameObject.SetActive(false);
	}

	private void onRestartGame()
	{
		Debug.Log("onRestartGame Level" + World.levelNum);
		LoadLevelScreen.LevelToLoad = World.levelNum;
		World.IsRestart = true;
		World.LoadLevel("LoadLevel", 0.5f);
	}

	private void onMainMenu()
	{
		Debug.Log("onMainMenu");
		World.LoadLevel("MainMenu", 0.5f);
	}
}

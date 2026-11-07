using FLD;
using UnityEngine;

public class PostCompleteScreen : MonoBehaviour
{
	public static LevelCompleteResult Result;

	[SerializeField]
	private MenuHeader _menuHeader;

	[SerializeField]
	private LeaderboardController _leaderboardController;

	public GameObject Flipper;

	public SimpleButton ButtonMainMenu;

	public SimpleButton ButtonTryAgain;

	public SimpleButton ButtonNextLevel;

	private CameraFade fade;

	private string sceneToGoTo;

	private ConfirmScreen _confirmScreen;

	private bool _isEnabled = true;

	private SimpleScreen.HandlerDelegate lastYes;

	private SimpleScreen.HandlerDelegate lastNo;

	private void Start()
	{
		DarkNebulaAdsManager.Instance.ShowAd("PostComplete").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		if (float.IsNaN(Result.CompletionTime) || Result.CompletionTime == 0f)
		{
			Result.Expert = UserSettings.Instance.IsExpertMode;
		}
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_confirmScreen = GetComponentInChildren<ConfirmScreen>();
		_confirmScreen.gameObject.SetActive(false);
		ButtonNextLevel.Text = "NEXT LEVEL".Localize();
		ButtonNextLevel.ButtonUpEvent += delegate
		{
			DoNextLevel();
		};
		ButtonTryAgain.Text = "TRY AGAIN".Localize();
		ButtonTryAgain.ButtonUpEvent += delegate
		{
			DoTryAgain();
		};
		ButtonMainMenu.Text = "MAIN MENU".Localize();
		ButtonMainMenu.ButtonUpEvent += delegate
		{
			DoMainMenu();
		};
		_leaderboardController.SetLevel(Result.LevelNum);
		_menuHeader.BackButton.gameObject.SetActive(false);
		_menuHeader.SetTitleID("Level complete".Localize().ToUpper(), true);
		_menuHeader.SetLevel(Result.LevelNum);
		SetupLeaderboardTitle();
	}

	private void SetupLeaderboardTitle()
	{
		// The old selector is disabled and sits behind the backdrop in this
		// scene. Reuse its label in the open slot above the embedded table.
		Flipper.SetActive(true);
		Flipper.transform.localPosition = new Vector3(0f, 150f, 0f);
		FlipperButton heading = Flipper.GetComponent<FlipperButton>();
		heading.Title.SelectedEntry = 1;
		heading.Init(true, "LOCAL", "GLOBAL", "LEADERBOARD");
		heading.UseTitleOnly();
		heading.Title.SetOriginalPos(Vector3.zero);
		heading.Title.Refresh();
	}

	private void ConfirmMainMenu()
	{
		Confirm("EXIT TO MAIN MENU?", () =>
		{
			DoMainMenu();
		}, () =>
		{
			CloseConfirm();
		});
	}

	private void Update()
	{
		if (_isEnabled && Input.GetKeyUp(KeyCode.Menu))
		{
			DoMainMenu();
		}
	}

	private void SetEnabled()
	{
		SetEnabled(true);
	}

	private void SetEnabled(bool on)
	{
		_leaderboardController.ShowLeaderboard(on);
		Flipper.SetActive(on);
		if (on)
		{
			_leaderboardController.SetLevel(Result.LevelNum);
		}
		_isEnabled = on;
		ButtonMainMenu.enabled = on;
		ButtonTryAgain.enabled = on;
		ButtonNextLevel.enabled = on;
	}

	private void OnDisable()
	{
		lastYes = null;
		lastNo = null;
		_confirmScreen = null;
	}

	private void Confirm(string titleId, SimpleScreen.HandlerDelegate yes, SimpleScreen.HandlerDelegate no)
	{
		SetEnabled(false);
		_confirmScreen.gameObject.SetActive(true);
		if (lastYes != null)
		{
			_confirmScreen.EventYES -= lastYes;
		}
		if (lastNo != null)
		{
			_confirmScreen.EventNO -= lastNo;
		}
		_confirmScreen.EventYES += yes;
		_confirmScreen.EventNO += no;
		lastYes = yes;
		lastNo = no;
		_confirmScreen.SetTitle(titleId);
	}

	private void CloseConfirm()
	{
		_confirmScreen.gameObject.SetActive(false);
		SetEnabled(true);
	}

	private void SetNextScene()
	{
		if (ContentGatingManager.Instance.IsLevelGated(LoadLevelScreen.LevelToLoad) && !ContentGatingManager.Instance.IsGateUnlocked(LoadLevelScreen.LevelToLoad))
		{
			sceneToGoTo = "LevelSelect";
		}
		else if (LoadLevelScreen.LevelToLoad == 11 || LoadLevelScreen.LevelToLoad >= 23)
		{
			sceneToGoTo = "LoadLevel";
		}
		else
		{
			sceneToGoTo = "BoosterSelection";
		}
	}

	private void DoNextLevel()
	{
		LoadLevelScreen.LevelToLoad = LevelCompleteScreen.Result.LevelNum + 1;
		SetNextScene();
		fade.FadeOut();
		Invoke("OnGotoScene", 0.6f);
	}

	private void DoTryAgain()
	{
		LoadLevelScreen.LevelToLoad = LevelCompleteScreen.Result.LevelNum;
		// Match the later release's retry flow: keep the chosen boosters and
		// restart directly instead of reopening their selection screen.
		sceneToGoTo="LoadLevel";
		fade.FadeOut();
		Invoke("OnGotoScene", 0.6f);
	}

	private void DoMainMenu()
	{
		sceneToGoTo = "MainMenu";
		fade.FadeOut();
		Invoke("OnGotoScene", 0.6f);
	}

	private void OnGotoScene()
	{
		if (sceneToGoTo == null)
		{
			Debug.LogError("OnGotoScene NULL");
			return;
		}
		Debug.Log("OnGotoScene " + sceneToGoTo);
		StubLoader.LevelToLoad = sceneToGoTo;
		Application.LoadLevel("StubLoader");
	}
}

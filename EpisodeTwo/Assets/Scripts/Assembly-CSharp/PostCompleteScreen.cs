using System;
using UnityEngine;

public class PostCompleteScreen : MonoBehaviour
{
	public static LevelCompleteResult Result;

	public GameObject Flipper;

	public GameObject ButtonFacebook;

	public GameObject ButtonMainMenu;

	public GameObject ButtonTryAgain;

	public GameObject ButtonNextLevel;

	public GameObject GfxLeaderboardView;

	private CameraFade fade;

	private string sceneToGoTo;

	private ConfirmScreen _confirmScreen;

	private LeaderboardView _leaderboardView;

	private bool _isEnabled = true;

	private SimpleScreen.HandlerDelegate lastYes;

	private SimpleScreen.HandlerDelegate lastNo;

	private void Start()
	{
		if (float.IsNaN(Result.CompletionTime) || Result.CompletionTime == 0f)
		{
			Result.Expert = UserSettings.I.expert;
		}
		DarkNebulaMetrics.StartLevelScreen(DarkNebulaMetrics.LevelScreens.PostComplete_, Result.LevelNum);
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_confirmScreen = GetComponentInChildren<ConfirmScreen>();
		_confirmScreen.gameObject.SetActive(false);
		SimpleText[] componentsInChildren = GetComponentsInChildren<SimpleText>();
		foreach (SimpleText simpleText in componentsInChildren)
		{
			simpleText.UseCurrentTextAsStringID();
		}
		Flipper.SetActive(true);
		Flipper.GetComponent<FlipperButton>().Init(true, "LOCAL", "GLOBAL", "LEADERBOARD");
		Flipper.GetComponent<FlipperButton>().UseTitleOnly();
		ButtonNextLevel.GetComponent<SimpleButton>().ButtonUpEvent += delegate
		{
			DoNextLevel();
		};
		ButtonTryAgain.GetComponent<SimpleButton>().ButtonUpEvent += delegate
		{
			Confirm("RESTART LEVEL?", () =>
			{
				DoTryAgain();
			}, () =>
			{
				CloseConfirm();
			});
		};
		ButtonMainMenu.GetComponent<SimpleButton>().ButtonUpEvent += delegate
		{
			ConfirmMainMenu();
		};
		ButtonFacebook.GetComponent<SimpleButton>().ButtonUpEvent += delegate
		{
			SetEnabled(false);
			Invoke("SetEnabled", 1f);
			DarkNebulaFacebook.PostLevelComplete(2, Result.LevelNum, Result.CompletionTime, Result.Expert, Result.Star);
		};
		ButtonFacebook.SetActive(BuildSettings.Current.FacebookEnabled);
		SaveToLocalLeaderboard();
		_leaderboardView = GfxLeaderboardView.GetComponent<LeaderboardView>();
		_leaderboardView.EventDifficultyChanged += FeedLeaderboardView;
		FeedLeaderboardView(Result.Expert);
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

	private void SaveToLocalLeaderboard()
	{
		if (Result.LevelNum >= 1 && Result.LevelNum <= UserSettings.I.GetMaxLevel() && LocalLeaderboard.IsValidTime(Result.CompletionTime))
		{
			LeaderboardEntryVO entry = new LeaderboardEntryVO
			{
				date = DateTime.Now,
				time = Result.CompletionTime,
				name = UserSettings.I.GetUserName(),
				expert = Result.Expert,
				star = (int)Result.Star
			};
			new LocalLeaderboard().AddEntry(entry, Result.Expert, Result.LevelNum);
		}
	}

	private void FeedLeaderboardView(bool pExpert)
	{
		_leaderboardView.SetList(new LocalLeaderboard().GetList(pExpert, Result.LevelNum), pExpert);
	}

	private void Update()
	{
		if (_isEnabled && Input.GetKeyUp(KeyCode.Menu))
		{
			ConfirmMainMenu();
		}
	}

	private void SetEnabled()
	{
		SetEnabled(true);
	}

	private void SetEnabled(bool on)
	{
		_isEnabled = on;
		ButtonFacebook.SetActive(on && BuildSettings.Current.FacebookEnabled);
		ButtonMainMenu.GetComponent<SimpleButton>().enabled = on;
		ButtonTryAgain.GetComponent<SimpleButton>().enabled = on;
		ButtonNextLevel.GetComponent<SimpleButton>().enabled = on;
	}

	private void OnDisable()
	{
		lastYes = null;
		lastNo = null;
		_confirmScreen = null;
		if (_leaderboardView != null) _leaderboardView.EventDifficultyChanged -= FeedLeaderboardView;
		_leaderboardView = null;
	}

	private void Confirm(string titleId, SimpleScreen.HandlerDelegate yes, SimpleScreen.HandlerDelegate no)
	{
		SetEnabled(false);
		_confirmScreen.gameObject.SetActive(true);
		_leaderboardView.gameObject.SetActive(false);
		Flipper.SetActive(false);
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
		_leaderboardView.gameObject.SetActive(true);
		Flipper.SetActive(true);
		SetEnabled(true);
	}

	private void DoNextLevel()
	{
		LoadLevelScreen.LevelToLoad = LevelCompleteScreen.Result.LevelNum + 1;
		sceneToGoTo = "LoadLevel";
		fade.FadeOut();
		Invoke("OnGotoScene", 0.6f);
	}

	private void DoTryAgain()
	{
		World.IsRestart = true;
		LoadLevelScreen.LevelToLoad = LevelCompleteScreen.Result.LevelNum;
		sceneToGoTo = "LoadLevel";
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
		StubLoader.LoadLevel(sceneToGoTo);
	}
}

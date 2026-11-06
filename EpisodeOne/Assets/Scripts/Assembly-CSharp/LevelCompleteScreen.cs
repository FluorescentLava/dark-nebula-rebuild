using Random = UnityEngine.Random;
using System;
using FLD;
using Palringo;
using UnityEngine;

public class LevelCompleteScreen : MonoBehaviour
{
	public const int SilverStarOffset = 20;

	[SerializeField]
	private MenuHeader _menuHeader;

	public static LevelCompleteResult Result;

	public static float PreviousBestCompletionTime = float.NaN;

	private static int[] parNormal = new int[23]
	{
		90, 58, 93, 105, 95, 65, 98, 82, 120, 147,
		1000, 89, 98, 100, 67, 106, 115, 108, 72, 90,
		87, 93, 1000
	};

	private static int[] parExpert = new int[23]
	{
		80, 41, 91, 100, 87, 46, 97, 81, 106, 130,
		1000, 90, 96, 98, 52, 100, 111, 105, 55, 93,
		83, 91, 1000
	};

	public GameObject TimeGroup;

	public GameObject ResultGroup;

	public GameObject OrbsGroup;

	public SimpleButton ContinueButton;

	public GameObject CameraObject;

	public AudioClip SfxTick;

	public AudioClip SfxStar;

	public AudioClip SfxStarBig;

	private CameraFade fade;

	private PlayerNameInput nameInput;
	private bool scoreSaved;

	private LevelCompleteSection[] sections;

	private int currentSection = -1;

	private bool _done;
	private bool _initialized;
	private bool _skipping;
	private bool _continuing;

	private bool _showingAd;

	private void Awake()
	{
		_showingAd = true;
		CalculateScore();
		DarkNebulaAdsManager.Instance.ShowAd("LevelComplete").Wait((string obj) =>
		{
			_showingAd = false;
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		Time.timeScale = 1f;
		fade = CameraObject.AddComponent<CameraFade>();

		if (Result.LevelNum == 0)
		{
			Result.LevelNum = 1;
			Result.CompletionTime = 125.24f;
			Result.Expert = false;
			Result.OrbsCollected = 40;
			Result.OrbsOnLevel = 50;
		}
		Setup();
		_initialized = true;
		fade.FadeComplete += OnFadeInComplete;
		fade.FadeIn();
	}

	private void Start()
	{
		_menuHeader.BackButton.gameObject.SetActive(false);
		_menuHeader.SetTitleID("Level complete".Localize().ToUpper(), true);
		_menuHeader.UpdateCurrentLevel();
	}

	private void Setup()
	{
		sections = new LevelCompleteSection[3];
		sections[0] = OrbsGroup.GetComponent<LevelCompleteSection>();
		sections[1] = TimeGroup.GetComponent<LevelCompleteSection>();
		sections[2] = ResultGroup.GetComponent<LevelCompleteSection>();
		LevelCompleteSection obj = sections[0];
		obj.EventComplete = (Action)Delegate.Combine(obj.EventComplete, new Action(OnNextSection));
		LevelCompleteSection obj2 = sections[1];
		obj2.EventComplete = (Action)Delegate.Combine(obj2.EventComplete, new Action(OnNextSection));
		LevelCompleteSection obj3 = sections[0];
		obj3.StarSound = (Action<bool>)Delegate.Combine(obj3.StarSound, new Action<bool>(OnPlayStarSound));
		LevelCompleteSection obj4 = sections[1];
		obj4.StarSound = (Action<bool>)Delegate.Combine(obj4.StarSound, new Action<bool>(OnPlayStarSound));
		LevelCompleteSection obj5 = sections[2];
		obj5.StarSound = (Action<bool>)Delegate.Combine(obj5.StarSound, new Action<bool>(OnPlayStarSound));
		LevelCompleteSection obj6 = sections[2];
		obj6.EventComplete = (Action)Delegate.Combine(obj6.EventComplete, new Action(OnDone));
		LevelCompleteSection obj7 = sections[0];
		obj7.Tick = (Action)Delegate.Combine(obj7.Tick, new Action(OnTick));
		LevelCompleteSection obj8 = sections[1];
		obj8.Tick = (Action)Delegate.Combine(obj8.Tick, new Action(OnTick));
		ContinueButton.Text = "CONTINUE".Localize();
		ContinueButton.ButtonUpEvent += OnContinue;
		foreach (tk2dSprite sprite in GetComponentsInChildren<tk2dSprite>(true))
		{
			if (sprite.Collection.GetSpriteIdByName("InputField", -1) < 0) continue;
			nameInput = PlayerNameInput.Create(transform, ContinueButton, sprite.Collection);
			break;
		}
		SetEnabled();
	}

	private void SetEnabled()
	{
		ContinueButton.gameObject.SetActive(false);
	}

	private void OnPlayStarSound(bool big)
	{
        GameHaptics.Play(HapticEvent.Star);
		Audio.Play("star" + Random.value, (!big) ? SfxStar : SfxStarBig);
	}

	private void OnDestroy()
	{
		_initialized = false;
		if (sections != null)
		{
			foreach (LevelCompleteSection section in sections)
			{
				if (section == null) continue;
				section.EventComplete -= OnNextSection;
				section.EventComplete -= OnDone;
				section.StarSound -= OnPlayStarSound;
				section.Tick -= OnTick;
			}
		}
		if (fade != null)
		{
			fade.FadeComplete -= OnFadeInComplete;
			fade.FadeComplete -= OnFadeInSuperStar;
			fade.FadeComplete -= OnFadeOutSuperStar;
		}
	}

	private void OnTick()
	{
        GameHaptics.Play(HapticEvent.Score);
		Audio.Play("Tick" + Random.value, SfxTick);
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
		if (!(sections[currentSection] == null))
		{
			sections[currentSection].StartAnimation();
		}
	}

	private void OnDone()
	{
		if (!_initialized || _done) return;
		_done = true;
		DarkNebulaAchievements.Instance.SyncAchievements();
		ContinueButton.gameObject.SetActive(true);
	}

	private void ShowSuperStar()
	{
		fade.FadeOut();
		fade.FadeComplete += OnFadeInSuperStar;
	}

	private void OnFadeInSuperStar()
	{
		base.gameObject.SetActive(false);
		fade.FadeComplete -= OnFadeInSuperStar;
		fade.FadeIn();
	}

	private void OnSuperStarClose()
	{
		fade.FadeComplete += OnFadeOutSuperStar;
		fade.FadeOut();
	}

	private void OnFadeOutSuperStar()
	{
		fade.FadeComplete -= OnFadeOutSuperStar;
		fade.FadeIn();
		base.gameObject.SetActive(true);
		SetEnabled();
		ContinueButton.gameObject.SetActive(true);
	}

	public static StarValue GetStarValue(float pTime, int pLevelNum, bool pExpert)
	{
		int levelParTime = GetLevelParTime(pLevelNum, pExpert);
		StarValue result = StarValue.Bronze;
		if (pTime < (float)(levelParTime + 20))
		{
			result = StarValue.Silver;
		}
		if (pTime <= (float)levelParTime)
		{
			result = StarValue.Gold;
		}
		return result;
	}

	public static int GetLevelParTimeForStar(StarValue pStarValue, int pLevelNum, bool pExpert)
	{
		int levelParTime = GetLevelParTime(pLevelNum, pExpert);
		int num = levelParTime + 20;
		int result = 0;
		switch (pStarValue)
		{
		case StarValue.Gold:
			result = levelParTime;
			break;
		case StarValue.Silver:
			result = num;
			break;
		}
		return result;
	}

	public static float GetTimeToNextStar(float pTime, int pLevelNum, bool pExpert)
	{
		StarValue starValue = GetStarValue(pTime, pLevelNum, pExpert) + 1;
		float result = 0f;
		switch (starValue)
		{
		case StarValue.Gold:
		{
			int levelParTimeForStar2 = GetLevelParTimeForStar(StarValue.Gold, Result.LevelNum, Result.Expert);
			result = pTime - (float)levelParTimeForStar2;
			break;
		}
		case StarValue.Silver:
		{
			int levelParTimeForStar = GetLevelParTimeForStar(StarValue.Gold, Result.LevelNum, Result.Expert);
			result = pTime - (float)levelParTimeForStar;
			break;
		}
		}
		return result;
	}

	private void SyncBestGlobalTime(float pBestLocalTime)
	{
		if (!DarkNebulaPalringo.IsAuthorized)
		{
			return;
		}
		Leaderboard leaderboard = DarkNebulaPalringo.Connect.GetLeaderboard(LeaderboardController.GetLeaderboardID(Result.LevelNum, Result.Expert));
		if (leaderboard == null)
		{
			return;
		}
		leaderboard.GetLocalPlayer(LeaderboardViewType.Alltime).Wait((Palringo.LeaderboardEntry pLeaderboardEntry) =>
		{
			if (pLeaderboardEntry != null)
			{
				float num = (float)pLeaderboardEntry.Score / 100000f;
				if (pBestLocalTime < num || num < 0f)
				{
					SecureInt pTime = Mathf.RoundToInt(pBestLocalTime * 100000f);
					Debug.Log("Synced server time with a local time that was better. Previous best local time that will be uploaded to server " + pBestLocalTime + ". Best server time " + num);
					SaveToGlobalLeaderboard(pTime);
				}
			}
		});
	}

	private void CalculateScore()
	{
		if (Result.LevelNum < 1 || Result.LevelNum > 23 || !LocalLeaderboard.IsValidTime(Result.CompletionTime)) return;
		Result.Star = GetStarValue(Result.CompletionTime, Result.LevelNum, Result.Expert);
		PreviousBestCompletionTime = UserProgress.Instance.GetBestTime(Result.LevelNum, Result.Expert);
		if (PreviousBestCompletionTime == 0f)
		{
			PreviousBestCompletionTime = Result.CompletionTime;
		}
		if (PreviousBestCompletionTime >= Result.CompletionTime)
		{
			UserProgress.Instance.SetBestTime(Result.CompletionTime, Result.LevelNum, Result.Expert);
			PreviousBestCompletionTime = Result.CompletionTime;
		}
		int levelStar = UserProgress.Instance.GetLevelStar(Result.LevelNum, Result.Expert);
		if (levelStar < (int)(Result.Star + 1))
		{
			UserProgress.Instance.SetLevelStar(Result.LevelNum, (int)(Result.Star + 1), Result.Expert);
		}
		DarkNebulaMetrics.LevelCompleted(Result.LevelNum, Result.CompletionTime, Result.Expert, !UserProgress.Instance.HaveCompletedLevel(Result.LevelNum, Result.Expert), AvatarManager.Instance.ActiveAvatar.Name);
		if (!UserProgress.Instance.HaveCompletedLevel(Result.LevelNum, Result.Expert))
		{
			DarkNebulaMetrics.LevelProgression(Mathf.Min(23, Result.LevelNum + 1), Result.Expert);
		}
		SecureInt pTime = Mathf.RoundToInt(Result.CompletionTime * 100000f);
		SaveToGlobalLeaderboard(pTime);
		if (UserProgress.Instance.IsTotalTimeUnlocked(Result.Expert))
		{
			SubmitTotalScoreToGlobalLeaderboard();
		}
	}

	public static void SubmitTotalScoreToGlobalLeaderboard()
	{
		if (!DarkNebulaPalringo.IsAuthorized) return;
		string leaderboardID = LeaderboardController.GetLeaderboardID(0, UserSettings.Instance.IsExpertMode);
		SecureInt score = Mathf.RoundToInt(UserProgress.Instance.GetTotalTime(UserSettings.Instance.IsExpertMode) * 100000f);
		Leaderboard leaderboard = DarkNebulaPalringo.Connect.GetLeaderboard(leaderboardID);
		leaderboard.SubmitScore(score).Wait((SubmitScoreResult obj) =>
		{
			Debug.Log("Submit total with result " + obj.Status);
		});
	}

	private void SaveToGlobalLeaderboard(SecureInt pTime)
	{
		if (!DarkNebulaPalringo.IsAuthorized) return;
		string leaderboardID = LeaderboardController.GetLeaderboardID(Result.LevelNum, Result.Expert);
		Leaderboard leaderboard = DarkNebulaPalringo.Connect.GetLeaderboard(leaderboardID);
		leaderboard.SubmitScore(pTime).Wait((SubmitScoreResult obj) =>
		{
			Debug.Log("Submit score to palringo with result " + obj.Status);
			LeaderboardController leaderboardController = UnityEngine.Object.FindObjectOfType<LeaderboardController>();
			if (leaderboardController != null)
			{
				leaderboardController.UpdateLeaderboard();
			}
		});
	}

	public static int GetLevelParTime(int level, bool pExpert)
	{
		if (level < 1 || parNormal.Length < level)
		{
			Debug.LogWarning("No par time for level " + level);
			return 222;
		}
		if (pExpert)
		{
			return parExpert[level - 1];
		}
		return parNormal[level - 1];
	}

	private void Update()
	{
		if (_initialized && !_showingAd && Input.GetMouseButton(0) && !_done)
		{
			_skipping = true;
			sections[0].SetComplete();
			sections[1].SetComplete();
			sections[2].SetComplete();
			_skipping = false;
			OnDone();
		}
	}

	private void OnContinue(SimpleButton button)
	{
		if (!_initialized || !_done || _continuing) return;
		_continuing = true;
		if (nameInput != null) nameInput.Commit();
		SaveLocalScore();
		Invoke("OnGotoLevelCompleteLeaderboard", 0.6f);
		fade.FadeOut();
	}

	private void SaveLocalScore()
	{
		// Save after confirming the nickname, once even if Continue is tapped twice.
		if (scoreSaved || Result.LevelNum < 1 || Result.LevelNum > 23 || !LocalLeaderboard.IsValidTime(Result.CompletionTime)) return;
		scoreSaved = true;
		new LocalLeaderboard().AddEntry(new LeaderboardEntryVO {
			date = DateTime.Now, time = Result.CompletionTime,
			name = UserSettings.Instance.GetUserName(), expert = Result.Expert,
			star = (int)Result.Star, level = Result.LevelNum
		}, Result.Expert, Result.LevelNum);
	}

	private void OnGotoLevelCompleteLeaderboard()
	{
		PostCompleteScreen.Result = Result;
		StubLoader.LevelToLoad = "PostComplete";
		Application.LoadLevel("StubLoader");
	}
}

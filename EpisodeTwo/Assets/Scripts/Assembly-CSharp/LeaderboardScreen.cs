using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeaderboardScreen : MonoBehaviour
{
	private enum TimePeriod
	{
		AllTime = 0,
		ThisWeek = 1,
		Today = 2
	}

	public GameObject GfxLeaderboardFlipper;

	public GameObject GfxLevelFlipper;

	public GameObject GfxTimePeriodFlipper;

	public GameObject GfxGoBackButton;

	public GameObject GfxLeaderboardView;

	private CameraFade fade;

	private int _levelIndex;

	private bool _expert;

	private LeaderboardView _leaderboardView;

	private bool _isEnabled = true;

	private void TestSerialization()
	{
		LeaderboardEntryVO leaderboardEntryVO = new LeaderboardEntryVO
		{
			name = "First!",
			time = 83.139f,
			expert = true,
			date = DateTime.Now
		};
		List<object> list = new List<object>();
		list.Add(LeaderboardEntry.Serialize(leaderboardEntryVO));
		string text = FugaJSON.jsonEncode(list);
		Debug.Log("json: " + text);
		Debug.Log("e: " + leaderboardEntryVO);
		object obj = FugaJSON.jsonDecode(text);
		if (obj is List<object>)
		{
			List<object> list2 = (List<object>)obj;
			{
				foreach (object item in list2)
				{
					Debug.Log("o=" + item);
					LeaderboardEntryVO e = LeaderboardEntry.Deserialize((Dictionary<string, object>)item);
					List<object> list3 = new List<object>();
					list3.Add(LeaderboardEntry.Serialize(e));
					string text2 = FugaJSON.jsonEncode(list3);
					Debug.Log("jsonr: " + text2);
				}
				return;
			}
		}
		Debug.Log("result != List");
	}

	private void Start()
	{
		bool expert = UserSettings.I.expert;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_leaderboardView = GfxLeaderboardView.GetComponent<LeaderboardView>();
		_leaderboardView.EventDifficultyChanged += FeedLeaderboardList;
		GfxGoBackButton.GetComponent<SimpleButton>().ButtonUpEvent += HandleButtonUpEvent;
		DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.MainMenu_Leaderboard);
		GfxLeaderboardFlipper.SetActive(false);
		List<string> list = new List<string>();
		int num = UserSettings.I.GetMaxLevel();
		if (!BuildSettings.Current.ExtendedLevelsEnabled)
		{
			num = UserSettings.I.GetPackMaxLevel(LevelPack.Original) - 1;
		}
		for (int i = 0; i < num; i++)
		{
			list.Add(Language.Get("LEVEL") + " " + (i + 1));
		}
		GfxLevelFlipper.GetComponent<FlipperButtonMultipleValues>().Init(0, list.ToArray(), "LEADERBOARD");
		GfxLevelFlipper.GetComponent<FlipperButtonMultipleValues>().EventValueChanged += LevelChanged;
		GfxTimePeriodFlipper.SetActive(false);
		_leaderboardView.SetUseLargeView();
		FeedLeaderboardList(expert);
	}

	private void OnDisable()
	{
		_leaderboardView.EventDifficultyChanged -= FeedLeaderboardList;
		_leaderboardView = null;
	}

	private void LevelChanged(int pLevelIndex)
	{
		_levelIndex = pLevelIndex;
		FeedLeaderboardList(_expert);
	}

	private void FeedLeaderboardList(bool pExpert)
	{
		_expert = pExpert;
		_leaderboardView.SetList(new LocalLeaderboard().GetList(_expert, _levelIndex + 1), _expert);
	}

	private void HandleButtonUpEvent(SimpleButton source)
	{
		StartCoroutine(LoadLevel("MainMenu"));
	}

	private void Update()
	{
		if (_isEnabled && Input.GetKeyUp(KeyCode.Escape))
		{
			StartCoroutine(LoadLevel("MainMenu"));
		}
	}

	private IEnumerator LoadLevel(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		_isEnabled = false;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}
}

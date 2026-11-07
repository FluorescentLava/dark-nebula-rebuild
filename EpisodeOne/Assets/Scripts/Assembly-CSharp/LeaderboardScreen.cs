using System.Collections;
using FLD;
using UnityEngine;

public class LeaderboardScreen : MonoBehaviour
{
	[SerializeField]
	private LeaderboardController _leaderboardController;

	public FlipperButtonMultipleValues GfxLevelFlipper;

	public GameObject GfxGoBackButton;

	private CameraFade fade;

	private bool _isEnabled = true;

	private void Start()
	{
		DarkNebulaAdsManager.Instance.ShowAd("Leaderboard").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		GfxGoBackButton.GetComponent<SimpleButton>().ButtonUpEvent += HandleButtonUpEvent;
		GfxGoBackButton.GetComponentInChildren<DynamicFont>().Text = "GO BACK".Localize();
		SetupLevelFlipper();
		_leaderboardController.SetLevel(GfxLevelFlipper.CurrentIndex);
	}

	private void SetupLevelFlipper()
	{
		bool flag = UserProgress.Instance.HasCompletedAllLevelsInAnyDifficulty();
		string[] array = new string[24];
		for (int i = 0; i < array.Length; i++)
		{
			if (i == 0)
			{
				array[i] = "TOTAL TIME".Localize();
				continue;
			}
			string text = "LEVEL_NR".Localize(i);
			array[i] = text;
		}
		int pCurrentKeyValue = 1;
		if (flag)
		{
			pCurrentKeyValue = 0;
		}
		GfxLevelFlipper.Init(pCurrentKeyValue, array, "LEADERBOARD");
		if (!flag)
		{
			GfxLevelFlipper.ForbiddenIndex = 0;
		}
		GfxLevelFlipper.EventValueChanged += LevelChanged;
	}

	private void OnDisable()
	{
		GfxLevelFlipper.EventValueChanged -= LevelChanged;
	}

	private void LevelChanged(int pLevel)
	{
		_leaderboardController.SetLevel(pLevel);
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

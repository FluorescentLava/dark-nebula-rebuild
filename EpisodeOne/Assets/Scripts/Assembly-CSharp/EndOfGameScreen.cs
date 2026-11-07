using System;
using System.Collections;
using FLD;
using UnityEngine;

public class EndOfGameScreen : MonoBehaviour
{
	[SerializeField]
	private SimpleButton _continueButton;

	[SerializeField]
	private DynamicFont _comparePalringoText;

	[SerializeField]
	private DynamicFont _totalTimeText;

	[SerializeField]
	private GetPalringoSimpleButton _palringoButton;

	private CameraFade fade;

	private void Start()
	{
		DarkNebulaAdsManager.Instance.ShowAd("EndOfGame").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_continueButton.Text = "CONTINUE".Localize();
		_continueButton.ButtonUpEvent += OnContinueClicked;
		float totalTime = UserProgress.Instance.GetTotalTime(UserSettings.Instance.IsExpertMode);
		_totalTimeText.Text = LevelCompleteSection.ToTimeString(totalTime);
		_comparePalringoText.Text = ((!DarkNebulaPalringo.IsAuthorized) ? "END GAME NOT CONNECTED".Localize() : "END GAME CONNECTED".Localize());
		GetPalringoSimpleButton palringoButton = _palringoButton;
		palringoButton.ButtonUpdated = (Action)Delegate.Combine(palringoButton.ButtonUpdated, new Action(HandleButtonUpdated));
	}

	private void HandleButtonUpdated()
	{
		if (DarkNebulaPalringo.IsAuthorized)
		{
			StartCoroutine(LoadLevel("Leaderboard"));
		}
	}

	private void OnContinueClicked(SimpleButton source)
	{
		_continueButton.ButtonUpEvent -= OnContinueClicked;
		if (DarkNebulaPalringo.IsAuthorized)
		{
			StartCoroutine(LoadLevel("Leaderboard"));
		}
		else
		{
			StartCoroutine(LoadLevel("MainMenu"));
		}
	}

	private IEnumerator LoadLevel(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}
}

using System.Collections;
using FLD;
using UnityEngine;

public class EndOfChapterOne : MonoBehaviour
{
	[SerializeField]
	private SimpleButton _continueButton;

	private CameraFade fade;

	private void Start()
	{
		DarkNebulaAdsManager.Instance.ShowAd("EndOfChapterOne").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		_continueButton.Text = "CONTINUE".Localize();
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_continueButton.ButtonUpEvent += OnContinueClicked;
	}

	private void OnContinueClicked(SimpleButton source)
	{
		_continueButton.ButtonUpEvent -= OnContinueClicked;
		StartCoroutine(LoadLevel("LevelSelect"));
	}

	private IEnumerator LoadLevel(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}
}

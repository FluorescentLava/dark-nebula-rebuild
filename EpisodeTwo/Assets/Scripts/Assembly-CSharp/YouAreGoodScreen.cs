using System.Collections;
using UnityEngine;

public class YouAreGoodScreen : MonoBehaviour
{
	private CameraFade fade;

	private bool _isLoadingLevel;

	private void Start()
	{
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		string text = Language.Get("YOU ARE REALLY GOOD!");
		text = text + "\n" + Language.Get("BUT ARE YOU GOOD ENOUGH");
		text = text + "\n" + Language.Get("TO RECEIVE GOLD STARS");
		text = text + "\n" + Language.Get("ON ALL THE LEVELS?");
		GetComponentInChildren<SimpleText>().SetText(text);
		DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.YouAreGood);
	}

	private void Update()
	{
		if (Input.GetMouseButtonUp(0) && !_isLoadingLevel)
		{
			_isLoadingLevel = true;
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

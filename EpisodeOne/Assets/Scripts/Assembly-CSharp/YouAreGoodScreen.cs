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
		string text = "TEXT NEEDED HERE";
		GetComponentInChildren<SimpleText>().SetText(text);
	}

	private void Update()
	{
		if (Input.GetMouseButtonUp(0) && !_isLoadingLevel)
		{
			_isLoadingLevel = true;
			StartCoroutine(LoadLevel("LevelSelect"));
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

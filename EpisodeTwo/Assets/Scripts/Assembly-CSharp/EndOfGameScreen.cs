using System.Collections;
using UnityEngine;

public class EndOfGameScreen : MonoBehaviour
{
	public GameObject btn0;

	public GameObject btn1;

	public GameObject title;

	private CameraFade fade;

	private void Start()
	{
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		title.GetComponentInChildren<SimpleText>().UseCurrentTextAsStringID();
		btn0.GetComponentInChildren<SimpleText>().UseCurrentTextAsStringID();
		btn0.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onPostOnFacebook;
		btn0.SetActive(BuildSettings.Current.FacebookEnabled);
		btn1.GetComponentInChildren<SimpleText>().UseCurrentTextAsStringID();
		btn1.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onMainMenu;
	}

	private void SetEnabled()
	{
		SetEnabled(true);
	}

	private void SetEnabled(bool on)
	{
		btn0.SetActive(BuildSettings.Current.FacebookEnabled && on);
		btn1.SetActive(on);
	}

	private void onPostOnFacebook(SimpleButton source)
	{
		SetEnabled(false);
		Invoke("SetEnabled", 1f);
		DarkNebulaFacebook.PostCompletedEpisodeTwoToStream(World.levelNum);
	}

	private IEnumerator LoadLevel(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}

	private void onMainMenu(SimpleButton source)
	{
		SetEnabled(false);
		if (GetShowYouAreGoodScreen())
		{
			StartCoroutine(LoadLevel("YouAreGood"));
		}
		else
		{
			StartCoroutine(LoadLevel("MainMenu"));
		}
	}

	private bool GetShowYouAreGoodScreen()
	{
		if (UserSettings.I.GetIsAllGold(1, 20, UserSettings.I.expert))
		{
			return false;
		}
		if (UserSettings.I.GetHasShownYouAreGood())
		{
			return false;
		}
		UserSettings.I.SetHasShownYouAreGood(true);
		return true;
	}
}

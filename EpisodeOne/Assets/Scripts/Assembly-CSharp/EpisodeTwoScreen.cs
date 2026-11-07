using System.Collections;
using UnityEngine;

public class EpisodeTwoScreen : MonoBehaviour
{
	public GameObject btn0;

	public GameObject btn1;

	public GameObject btn2;

	private CameraFade fade;

	private bool _isEnabled = true;

	private void Start()
	{
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		btn0.GetComponentInChildren<SimpleText>().SetStringID("VIEW TRAILER");
		btn0.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onViewTrailer;
		btn1.GetComponentInChildren<SimpleText>().SetStringID("BUY EP2");
		btn1.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onBuyEpisodeTwo;
		btn0.transform.position = btn1.transform.position;
		btn1.gameObject.SetActive(false);
		btn2.GetComponentInChildren<SimpleText>().SetStringID("GO BACK");
		btn2.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onMainMenu;
	}

	private void SetEnabled(bool on)
	{
		_isEnabled = on;
		btn0.GetComponentInChildren<SimpleButton>().enabled = on;
		btn2.GetComponentInChildren<SimpleButton>().enabled = on;
	}

	private void onViewTrailer(SimpleButton source)
	{
		Application.OpenURL("http://www.youtube.com/watch?v=-oTR_MO4k1Y");
	}

	private void onBuyEpisodeTwo(SimpleButton source)
	{
	}

	private IEnumerator LoadLevel(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		fade.FadeOut();
		yield return new WaitForSeconds(1f);
		Application.LoadLevel(levelName);
	}

	private void onMainMenu(SimpleButton source)
	{
		MainMenu();
	}

	private void MainMenu()
	{
		SetEnabled(false);
		StartCoroutine(LoadLevel("MainMenu"));
	}

	private void Update()
	{
		if (_isEnabled && (Input.GetKeyUp(KeyCode.Menu) || Input.GetKeyUp(KeyCode.Escape)))
		{
			MainMenu();
		}
	}
}

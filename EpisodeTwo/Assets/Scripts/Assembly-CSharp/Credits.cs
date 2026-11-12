using System.Collections;
using UnityEngine;

public class Credits : MonoBehaviour
{
	private CameraFade fade;

	private bool exiting;

	public static bool GoToEndOfGame;

	private void Start()
	{
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		StartCoroutine(PlayMovie());
	}

	private IEnumerator PlayMovie()
	{
		StoryMoviePlayer.Playback movie = StoryMoviePlayer.Play(this, StoryMoviePlayer.Extro);
		while (!movie.IsDone) yield return null;
		while (StoryMoviePlayer.SkipInputHeld) yield return null;
		yield return null;
		if (!isActiveAndEnabled) yield break;
		if (movie.WasHandled || movie.Outcome == StoryMoviePlayer.Result.Failed) ExitScreen();
	}

	private void ExitScreen()
	{
		if (exiting) return;
		exiting = true;
		if (GoToEndOfGame)
		{
			GoToEndOfGame = false;
			StartCoroutine(LoadLevel("EndOfGame"));
		}
		else
		{
			StartCoroutine(LoadLevel("MainMenu"));
		}
		SleepTimeoutManager.AllowSleepIn(60f);
	}

	private IEnumerator LoadLevel(string levelName)
	{
		if (!SceneTransitionInput.TryBegin()) yield break;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}
}

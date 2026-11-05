using System.Collections;
using FLD;
using UnityEngine;

public class Credits : MonoBehaviour
{
	private const float FadeDuration = 1f;

	private const float TitleNameDuration = 2f;

	public AudioClip Music;

	public tk2dSprite FLDLogo;

	public tk2dTextMesh TitlePrefab;

	public tk2dTextMesh NamePrefab;

	private GameObject TextContainer;

	private float cursor;

	private bool ScrollText;

	private CameraFade fade;

	private bool CanExit;

	private bool playingMovie;

	private bool exiting;

	public static bool GoToEndOfGame;

	private void Start()
	{
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		playingMovie = true;
		StartCoroutine(PlayMovie());
	}

	private IEnumerator PlayMovie()
	{
		StoryMoviePlayer.Playback movie = StoryMoviePlayer.Play(this, StoryMoviePlayer.Extro);
		while (!movie.IsDone) yield return null;
		while (StoryMoviePlayer.SkipInputHeld) yield return null;
		yield return null;
		if (!isActiveAndEnabled) yield break;
		playingMovie = false;
		if (movie.WasHandled) ExitScreen();
		else if (movie.Outcome == StoryMoviePlayer.Result.Failed) StartCredits();
	}

	private void StartCredits()
	{
		Audio.ChangeMusicWithFade(Music, 0.6f, 1.5f);
		FLDLogo.gameObject.SetActive(false);
		Invoke("ExitScreen", 103.5f);
		CreateTexts();
		fade.FadeIn(7f);
		DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.Credits);
		StartCoroutine(Play());
		SleepTimeoutManager.NeverSleep();
	}

	private IEnumerator Play()
	{
		yield return new WaitForSeconds(2f);
		CanExit = true;
		yield return new WaitForSeconds(3f);
		yield return StartCoroutine(ShowSprite(FLDLogo, 3f));
		yield return new WaitForSeconds(1.5f);
		yield return StartCoroutine(ShowTitleName("Producer", "Magnus Alm", 3f));
		yield return new WaitForSeconds(1.5f);
		yield return StartCoroutine(ShowTitleName("Art & Design", "Anders Hejdenberg", 3f));
		yield return new WaitForSeconds(1.5f);
		yield return StartCoroutine(ShowTitleName("Music & Sound", "Calle Hansson", 3f));
		yield return new WaitForSeconds(1.5f);
		yield return StartCoroutine(ShowTitleName("Programming", "Christoffer Enedahl", 3f));
		yield return new WaitForSeconds(1.5f);
		yield return StartCoroutine(ShowTitleName("HD Graphics", "Patrik Liljecrantz\nIselin Kjelsvik", 3f));
		yield return new WaitForSeconds(1.5f);
		yield return StartCoroutine(ShowTitleName("Extended Levels", "Toivo SÄwen\nJimmy Öman", 3f));
		ScrollText = true;
	}

	private void CreateTexts()
	{
		cursor = 0f;
		TextContainer = new GameObject();
		TextContainer.name = "TextContainer";
		CreateTitle("Original Programming");
		CreateName("Peter BjÖrklund");
		CreateName("Johan Knutzen");
		CreateName("Shahrouz Zolfaghari");
		CreateSpace();
		CreateTitle("Additional Programming");
		CreateName("Maciek Drejak");
		CreateName("Jonathan JanesjÖ");
		CreateSpace();
		CreateTitle("Loading Pictures");
		CreateName("Robin Olausson");
		CreateSpace();
		CreateTitle("Testers");
		CreateName("Emanuel Garnheim");
		CreateName("Johan Lundqvist");
		CreateName("Johan Peitz");
		CreateName("Patrik Liljecrantz");
		CreateName("Peter Johansson");
		CreateSpace();
		CreateTitle("Thanks To");
		CreateName("Our families");
		CreateName("1337 Design");
		CreateName("Orbotix");
		CreateSpace();
		TitlePrefab.gameObject.SetActive(false);
		NamePrefab.gameObject.SetActive(false);
		TextContainer.transform.Translate(0f, -290f, 0f);
	}

	private void Update()
	{
		if (playingMovie || exiting) return;
		if (ScrollText)
		{
			TextContainer.transform.Translate(0f, Time.deltaTime * 25f, 0f);
		}
		if (CanExit && (Input.GetKeyUp(KeyCode.Escape) || Input.GetMouseButton(0)))
		{
			ExitScreen();
		}
	}

	private void ExitScreen()
	{
		if (exiting) return;
		exiting = true;
		CancelInvoke("ExitScreen");
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
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}

	private IEnumerator ShowTitleName(string pTitle, string pName, float pDuration)
	{
		TitlePrefab.text = pTitle.ToUpper();
		TitlePrefab.Commit();
		TitlePrefab.gameObject.SetActive(true);
		NamePrefab.text = pName.ToUpper();
		NamePrefab.Commit();
		NamePrefab.gameObject.SetActive(true);
		ChangeAlpha(TitlePrefab, -1f);
		ChangeAlpha(NamePrefab, -1f);
		yield return StartCoroutine(FadeTextIn());
		yield return new WaitForSeconds(pDuration);
		yield return StartCoroutine(FadeTextOut());
		TitlePrefab.gameObject.SetActive(false);
		NamePrefab.gameObject.SetActive(false);
	}

	private IEnumerator ShowSprite(tk2dSprite pSprite, float pDuration)
	{
		pSprite.color = new Color(1f, 1f, 1f, 0f);
		pSprite.gameObject.SetActive(true);
		yield return StartCoroutine(FadeIn(pSprite));
		yield return new WaitForSeconds(pDuration);
		yield return StartCoroutine(FadeOut(pSprite));
		pSprite.gameObject.SetActive(false);
	}

	private IEnumerator FadeIn(tk2dSprite pSprite)
	{
		while (pSprite.color.a < 1f)
		{
			Color col = pSprite.color;
			col.a += Time.deltaTime / 1f;
			pSprite.color = col;
			yield return null;
		}
	}

	private IEnumerator FadeOut(tk2dSprite pSprite)
	{
		while (pSprite.color.a > 0f)
		{
			Color col = pSprite.color;
			col.a -= Time.deltaTime / 1f;
			pSprite.color = col;
			yield return null;
		}
	}

	private IEnumerator FadeTextIn()
	{
		while (TitlePrefab.color.a < 1f)
		{
			ChangeAlpha(TitlePrefab, Time.deltaTime / 1f);
			ChangeAlpha(NamePrefab, Time.deltaTime / 1f);
			yield return null;
		}
		yield return null;
	}

	private IEnumerator FadeTextOut()
	{
		while (TitlePrefab.color.a > 0f)
		{
			ChangeAlpha(TitlePrefab, (0f - Time.deltaTime) / 1f);
			ChangeAlpha(NamePrefab, (0f - Time.deltaTime) / 1f);
			yield return null;
		}
		yield return null;
	}

	private void ChangeAlpha(tk2dTextMesh pText, float pChange)
	{
		Color color = pText.color;
		color.a += pChange;
		pText.color = color;
		pText.Commit();
	}

	private void CreateTitle(string title)
	{
		CreateText(TitlePrefab, title, 25f);
	}

	private void CreateName(string name)
	{
		CreateText(NamePrefab, name, 38f);
	}

	private void CreateText(tk2dTextMesh pPrefab, string text, float space)
	{
		tk2dTextMesh tk2dTextMesh2 = Object.Instantiate(pPrefab) as tk2dTextMesh;
		tk2dTextMesh2.text = text.ToUpper();
		tk2dTextMesh2.Commit();
		tk2dTextMesh2.transform.parent = TextContainer.transform;
		tk2dTextMesh2.transform.position = new Vector3(0f, cursor, 0f);
		CreateSpace(space);
	}

	private void CreateSpace(float space = 30f)
	{
		cursor -= space;
	}
}

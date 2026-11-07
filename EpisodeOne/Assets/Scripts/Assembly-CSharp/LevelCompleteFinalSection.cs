using FLD;
using UnityEngine;

public class LevelCompleteFinalSection : LevelCompleteSection
{
	[SerializeField]
	private tk2dSprite _star;

	[SerializeField]
	private DynamicFont _nextStarText;

	[SerializeField]
	private tk2dSprite _nextStarIcon;

	[SerializeField]
	private GameObject _subContainer;

	private bool done;

	public override void Start()
	{
        _nextStarText.FontRefreshed += AlignNextStarIcon;
		_nextStarIcon.gameObject.SetActive(false);
		_nextStarText.gameObject.SetActive(false);
		_star.transform.localScale = new Vector3(3f, 3f, 3f);
		base.Start();
		animationDuration -= starAnimationBeginTime;
		starAnimationBeginTime = 0f;
		_star.gameObject.SetActive(false);
		Value.gameObject.SetActive(false);
		_subContainer.SetActive(false);
	}

	public override void Update()
	{
		base.Update();
		if (startTime == 0f || done)
		{
			return;
		}
		done = true;
		Debug.Log("LevelCompleteScreen.Result.Star " + LevelCompleteScreen.Result.Star);
		Value.Text = GetResultText();
		if (LevelCompleteScreen.Result.Star != StarValue.Gold)
		{
			Timer.Delay(0.4f, () =>
			{
				ShowNextStarTime();
			});
		}
		ShowStar();
	}

	private void ShowNextStarTime()
	{
		string sprite = string.Format("Skillstars{0}{1}/{2}", (!LevelCompleteScreen.Result.Expert) ? string.Empty : "Expert", string.Empty, (int)(LevelCompleteScreen.Result.Star + 1));
		_nextStarIcon.SetSprite(sprite);
		_nextStarIcon.gameObject.SetActive(true);
		_nextStarText.gameObject.SetActive(true);
		float timeToNextStar = LevelCompleteScreen.GetTimeToNextStar(LevelCompleteScreen.Result.CompletionTime, LevelCompleteScreen.Result.LevelNum, LevelCompleteScreen.Result.Expert);
		_nextStarText.Text = "TIME TO NEXT".Localize(LevelCompleteSection.ToTimeString(timeToNextStar));
	}

    private void AlignNextStarIcon()
    {
        // Keep the target rating on the same row as its time, leaving the
        // nickname title and field their own space below the result.
        Renderer text = _nextStarText.GetComponent<Renderer>();
        Renderer icon = _nextStarIcon.GetComponent<Renderer>();
        Vector3 position = _nextStarIcon.transform.position;
        position.x = text.bounds.max.x + icon.bounds.extents.x + 6f;
        position.y = _nextStarText.InkCenterWorldY;
        _nextStarIcon.transform.position = position;
    }

    private void OnDestroy()
    {
        if (_nextStarText != null) _nextStarText.FontRefreshed -= AlignNextStarIcon;
    }

	private void ShowStar()
	{
		string sprite = string.Format("Skillstars{0}{1}/{2}", (!LevelCompleteScreen.Result.Expert) ? string.Empty : "Expert", "Large", (int)LevelCompleteScreen.Result.Star);
		_star.SetSprite(sprite);
		_star.gameObject.SetActive(true);
		Go.to(_star.transform, 0.3f, new GoTweenConfig().scale(0.7f).onComplete(delegate
		{
			StarSound(true);
			Value.gameObject.SetActive(true);
			_subContainer.SetActive(true);
		}));
	}

	private string GetResultText()
	{
		string text = ((!UserSettings.Instance.IsExpertMode) ? string.Empty : "EXPERT ");
		switch (LevelCompleteScreen.Result.Star)
		{
		case StarValue.Silver:
			return (text + "SILVER STAR!").Localize();
		case StarValue.Gold:
			return (text + "GOLD STAR!").Localize();
		default:
			return (text + "BRONZE STAR!").Localize();
		}
	}
}

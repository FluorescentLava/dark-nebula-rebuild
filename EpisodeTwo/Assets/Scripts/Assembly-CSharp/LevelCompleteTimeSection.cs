using UnityEngine;

public class LevelCompleteTimeSection : LevelCompleteSection
{
	public GameObject TitleBest;

	public GameObject TitleBestValue;

	private bool done;

	public override bool IsCounting
	{
		get { return startTime != 0f && !done && LevelCompleteScreen.Result.CompletionTime > 0f && Time.time - startTime < TickDuration; }
	}

	public override void Start()
	{
		base.Start();
		Title.GetComponent<SimpleText>().SetStringID("YOUR TIME");
		TitleBest.GetComponent<SimpleText>().SetStringID("YOUR BEST TIME");
		TitleBestValue.GetComponent<SimpleText>().SetText(ToTimeString(LevelCompleteScreen.PreviousBestCompletionTime));
		TitleBest.SetActive(false);
		TitleBestValue.SetActive(false);
		base.EventStar += OnStar;
	}

	private void OnDisable()
	{
		base.EventStar -= OnStar;
	}

	public override void SetComplete()
	{
		base.SetComplete();
		OnStar(true);
	}

	private void OnStar(bool big)
	{
		TitleBest.SetActive(true);
		TitleBest.GetComponent<Renderer>().enabled = true;
		TitleBestValue.SetActive(true);
		TitleBestValue.GetComponent<Renderer>().enabled = true;
	}

	public override void Update()
	{
		base.Update();
		if (startTime != 0f && !done)
		{
			float num = Time.time - startTime;
			float num2 = Mathf.Min(1f, num / TickDuration);
			float time = LevelCompleteScreen.Result.CompletionTime * num2;
			Value.GetComponent<SimpleText>().SetText(ToTimeString(time));
			if (num2 == 1f)
			{
				done = true;
			}
		}
	}

	public static string ToTimeString(float time)
	{
		float num = time % 60f;
		return ((int)((time - num) / 60f)).ToString("D2") + ":" + num.ToString("00.000");
	}
}

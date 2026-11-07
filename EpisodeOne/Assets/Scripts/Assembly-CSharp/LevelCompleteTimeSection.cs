using FLD;
using UnityEngine;

public class LevelCompleteTimeSection : LevelCompleteSection
{
	[SerializeField]
	private DynamicFont _penaltyText;

	public DynamicFont TitleBest;

	public DynamicFont TitleBestValue;

	private bool done;

	private int _previousOrbCount;

	private int totalMissingOrbs;

	public override bool IsCounting
	{
		get { return startTime != 0f && !done && totalMissingOrbs > 0 && Time.time - startTime < TickDuration; }
	}

	public override void Start()
	{
		base.Start();
		Title.Text = "YOUR TIME".Localize();
		TitleBest.Text = "YOUR BEST TIME".Localize();
		TitleBestValue.Text = LevelCompleteSection.ToTimeString(LevelCompleteScreen.PreviousBestCompletionTime);
		TitleBest.gameObject.SetActive(false);
		TitleBestValue.gameObject.SetActive(false);
		_penaltyText.gameObject.SetActive(false);
		totalMissingOrbs = LevelCompleteScreen.Result.OrbsOnLevel - LevelCompleteScreen.Result.OrbsCollected;
		Value.Text = LevelCompleteSection.ToTimeString(LevelCompleteScreen.Result.CompletionTime - (float)totalMissingOrbs);
	}

	private void ShowBestTime()
	{
		TitleBest.gameObject.SetActive(true);
		TitleBestValue.gameObject.SetActive(true);
		Timer.Delay(0.5f, () =>
		{
			base.SetComplete();
		});
	}

	public override void Update()
	{
		base.Update();
		if (startTime == 0f || done)
		{
			return;
		}
		if (totalMissingOrbs == 0)
		{
			SetDone();
			return;
		}
		_penaltyText.gameObject.SetActive(true);
		float num = Time.time - startTime;
		float num2 = Mathf.Min(1f, num / TickDuration);
		int num3 = Mathf.CeilToInt((float)totalMissingOrbs * num2);
		int num4 = totalMissingOrbs - num3;
		if (num3 != _previousOrbCount)
		{
			_previousOrbCount = num3;
			if (Tick != null)
			{
				Tick();
			}
		}
		float time = LevelCompleteScreen.Result.CompletionTime - (float)totalMissingOrbs + (float)num3;
		Value.Text = LevelCompleteSection.ToTimeString(time);
		_penaltyText.Text = "PENALTY TEXT SMALL".Localize(num4);
		if (num2 >= 1f)
		{
			SetDone();
		}
	}

	private void SetDone()
	{
		done = true;
		_penaltyText.gameObject.SetActive(false);
		StarSound(false);
		ShowBestTime();
	}
}

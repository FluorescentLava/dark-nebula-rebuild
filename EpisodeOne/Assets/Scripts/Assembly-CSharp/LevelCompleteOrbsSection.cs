using FLD;
using UnityEngine;

public class LevelCompleteOrbsSection : LevelCompleteSection
{
	[SerializeField]
	private DynamicFont _penaltyText;

	[SerializeField]
	private DynamicFont _excellentText;

	private int _previousOrbCount;

	private bool done;

	public override bool IsCounting
	{
		get { return startTime != 0f && !done && LevelCompleteScreen.Result.OrbsOnLevel > LevelCompleteScreen.Result.OrbsCollected && Time.time - startTime < TickDuration; }
	}

	public override void Start()
	{
		base.Start();
		Title.Text = "CELLS COLLECTED".Localize();
		Value.Text = "CELLS".Localize(LevelCompleteScreen.Result.OrbsCollected, LevelCompleteScreen.Result.OrbsOnLevel);
		_penaltyText.gameObject.SetActive(false);
		_excellentText.gameObject.SetActive(false);
	}

	private void SetDone()
	{
		StarSound(false);
		done = true;
		Timer.Delay(1f, () =>
		{
			SetComplete();
		});
	}

	public override void Update()
	{
		base.Update();
		if (startTime == 0f || done)
		{
			return;
		}
		float num = Time.time - startTime;
		float num2 = Mathf.Min(1f, num / TickDuration);
		int num3 = LevelCompleteScreen.Result.OrbsOnLevel - LevelCompleteScreen.Result.OrbsCollected;
		if (num3 == 0)
		{
			_excellentText.gameObject.SetActive(true);
			SetDone();
			return;
		}
		num3 = Mathf.CeilToInt((float)num3 * num2);
		if (num3 != _previousOrbCount)
		{
			_previousOrbCount = num3;
			if (Tick != null)
			{
				Tick();
			}
		}
		_penaltyText.gameObject.SetActive(true);
		_penaltyText.Text = "PENALTY TEXT".Localize(num3);
		if (num2 == 1f)
		{
			SetDone();
		}
	}
}

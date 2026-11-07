using System;
using FLD;
using UnityEngine;

public class LevelCompleteSection : MonoBehaviour
{
	internal const float TickDuration = 1.6666666f;

	public DynamicFont Title;

	public DynamicFont Value;

	public GameObject Container;

	public Action EventComplete;

	public Action Tick;

	public Action<bool> StarSound;

	internal float startTime;

	internal float animationDuration = 4f;

	internal float starAnimationBeginTime = 2.5f;

	private bool _sectionDone;

	public virtual bool IsCounting { get { return false; } }

	public virtual void Start()
	{
		Container.SetActive(false);
	}

	public virtual void SetComplete()
	{
		if (!_sectionDone)
		{
			if (startTime == 0f)
			{
				StartAnimation();
			}
			startTime = Time.time - 6000f;
			Update();
		}
	}

	public virtual void Update()
	{
		if (startTime != 0f && !_sectionDone)
		{
			float num = Time.time - startTime;
			if (num > animationDuration)
			{
				_sectionDone = true;
				EventComplete();
			}
		}
	}

	private void SetupStartTime()
	{
		startTime = Time.time;
	}

	public void StartAnimation()
	{
		if (startTime == 0f)
		{
			Timer.Delay(0.5f, () =>
			{
				SetupStartTime();
			});
			Container.SetActive(true);
		}
	}

	public static string ToTimeString(float time)
	{
		float num = time % 60f;
		return ((int)((time - num) / 60f)).ToString("D2") + ":" + num.ToString("00.000");
	}
}

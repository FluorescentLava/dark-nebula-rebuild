using UnityEngine;

public class LevelCompleteSection : MonoBehaviour
{
	public delegate void Signal();

	public delegate void SignalStar(bool big);

	internal const float TickDuration = 1.6666666f;

	public GameObject Title;

	public GameObject Value;

	public GameObject Star;

	public StarValue StarResult;

	internal float startTime;

	internal float animationDuration = 4f;

	internal float starAnimationBeginTime = 2.5f;

	private float _starScale;

	private bool _done;

	private bool _playSound = true;

	public event Signal EventComplete;

	public event Signal EventStartTick;

	public event SignalStar EventStar;

	public virtual bool IsCounting { get { return false; } }

	public virtual void Start()
	{
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			renderer.enabled = false;
		}
		if (Star != null)
		{
			_starScale = Star.transform.localScale.x;
		}
	}

	public virtual void SetComplete()
	{
		if (!_done)
		{
			_playSound = false;
			if (startTime == 0f)
			{
				StartAnimation();
			}
			startTime = Time.time - 6000f;
			Update();
		}
	}

	private void PlayTick()
	{
		if (_playSound && EventStartTick != null)
		{
			EventStartTick();
		}
	}

	public virtual void Update()
	{
		if (startTime == 0f || _done)
		{
			return;
		}
		float num = Time.time - startTime;
		if (num > starAnimationBeginTime && Star != null)
		{
			if (_playSound)
			{
				EventStar(StarResult == StarValue.Gold);
				_playSound = false;
			}
			Star.GetComponent<Renderer>().enabled = true;
			float num2 = 1f - Mathf.Min(1f, (num - starAnimationBeginTime) / 0.1666f);
			Star.transform.localScale = Vector3.one * (_starScale + num2 * 4f);
			if (num2 == 0f)
			{
				Star = null;
			}
		}
		if (num > animationDuration)
		{
			_done = true;
			EventComplete();
		}
	}

	public void StartAnimation()
	{
		if (startTime != 0f)
		{
			return;
		}
		startTime = Time.time;
		Renderer[] componentsInChildren = GetComponentsInChildren<Renderer>();
		foreach (Renderer renderer in componentsInChildren)
		{
			renderer.enabled = true;
		}
		if (Star != null)
		{
			string arg = string.Empty;
			if (base.gameObject.name == "Result")
			{
				arg = "Large";
			}
			string text = string.Format("Skillstars{0}{1}/{2}", (!LevelCompleteScreen.Result.Expert) ? string.Empty : "Expert", arg, (int)StarResult);
			Star.GetComponent<tk2dSprite>().spriteId = Star.GetComponent<tk2dSprite>().GetSpriteIdByName(text);
			Star.GetComponent<Renderer>().enabled = false;
		}
		PlayTick();
	}
}

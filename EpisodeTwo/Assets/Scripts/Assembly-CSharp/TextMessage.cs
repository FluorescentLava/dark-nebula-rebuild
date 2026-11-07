using UnityEngine;

public class TextMessage : Entity
{
	private const float ANIMATION_TIME = 1f;

	public tk2dAnimatedSprite GfxFlare;

	public tk2dAnimatedSprite GfxGlow;

	private tk2dTextMesh _textMesh;

	private float animationTimer;

	private float glowEffectScale = 0.75f;

	private float _originalSpacing;

	private Vector3 _originalCharScale;

	private bool _inited;
	private bool _playing;

	private void Init()
	{
		if (!_inited)
		{
			_inited = true;
			_textMesh = GetComponent<tk2dTextMesh>();
			_originalSpacing = _textMesh.spacing;
			_originalCharScale = _textMesh.charScale;
			base.alwaysAlive = true;
			freezable = false;
		}
	}

	public void SetMessage(string message, bool red = false)
	{
		Init();
		_playing = true;
		animationTimer = 0f;
		base.transform.localPosition = new Vector3(0f, 120f, 0f);
		_textMesh.text = message;
		_textMesh.spacing = _originalSpacing;
		_textMesh.charScale = _originalCharScale;
		_textMesh.Commit();
		if (red)
		{
			GfxFlare.Play("TextFlareRed");
			GfxGlow.Play("TextGlowRed");
		}
	}

	public void StopMessage() { _playing = false; }

	private void Update()
	{
		if (_playing)
		{
			animationTimer += Time.deltaTime;
			float num = animationTimer / 1f;
			float num2 = num * 620f - 150f;
			GfxFlare.transform.localPosition = new Vector3(num2 - 160f, 0f, 0f);
			GfxFlare.transform.localScale = Vector3.one * 10f * glowEffectScale;
			num2 = num * 920f - 300f;
			GfxGlow.transform.localPosition = new Vector3(160f - num2, 0f, 0f);
			GfxGlow.transform.localScale = Vector3.one * 5f * glowEffectScale;
			_textMesh.spacing = animationTimer * 1.6f;
			if (num > 2.5f)
			{
				_textMesh.charScale = Vector3.one * (3f - num) * 1.8f;
			}
			_textMesh.text = _textMesh.text;
			_textMesh.Commit();
			if (animationTimer > 3f)
			{
				animationTimer = 0f;
				_playing = false;
				Hud owner = GetComponentInParent<Hud>();
				if (owner != null) owner.FinishMessage(this);
				else ObjectPool.instance.PoolObject(this);
			}
		}
	}

	public override void OnReturnedToPool()
	{
		base.OnReturnedToPool();
		base.enabled = false;
	}

	public override void OnTakenFromPool()
	{
		base.OnTakenFromPool();
		base.enabled = true;
	}
}

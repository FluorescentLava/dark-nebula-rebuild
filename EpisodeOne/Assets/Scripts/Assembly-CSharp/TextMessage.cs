using UnityEngine;

public class TextMessage : Entity
{
	private const float ANIMATION_TIME = 1f;

	public tk2dAnimatedSprite GfxFlare;

	public tk2dAnimatedSprite GfxGlow;

	public DynamicFont _textMesh;

	private float animationTimer;

	private float glowEffectScale = 0.75f;

	private float _originalSpacing;

	private Vector3 _originalCharScale;

	private bool _inited;
	private bool _initialized;

	private void Init()
	{
		if (!_initialized)
		{
			_initialized = true;
			_originalCharScale = _textMesh.transform.localScale;
		}
		_inited = true;
	}

	public void SetMessage(string message, bool red = false)
	{
		Init();
		animationTimer = 0f;
		base.transform.localPosition = new Vector3(0f, (float)Constants.VirtualScreenHeight * 0.25f, 0f);
		_textMesh.transform.localScale = _originalCharScale;
		_textMesh.Text = message;
        _textMesh.EnableMessageGradient();
		if (red)
		{
			GfxFlare.Play("TextFlareRed");
			GfxGlow.Play("TextGlowRed");
		}
	}

	public void StopMessage() { _inited = false; }

	private void Update()
	{
		if (_inited)
		{
			animationTimer += Time.deltaTime;
			float num = animationTimer / 1f;
			float num2 = num * 620f - 150f;
			GfxFlare.transform.localPosition = new Vector3(num2 - 160f, 0f, 0f);
			GfxFlare.transform.localScale = Vector3.one * 10f * glowEffectScale;
			num2 = num * 920f - 300f;
			GfxGlow.transform.localPosition = new Vector3(160f - num2, 0f, 0f);
			GfxGlow.transform.localScale = Vector3.one * 5f * glowEffectScale;
			if (num > 2.5f)
			{
				_textMesh.transform.localScale = Vector3.one * (3f - num) * 1.8f;
			}
			if (animationTimer > 3f)
			{
				animationTimer = 0f;
				_inited = false;
				Hud owner = GetComponentInParent<Hud>();
				if (owner != null) owner.FinishMessage(this);
				else ObjectPool.instance.PoolObject(this);
			}
		}
	}
}

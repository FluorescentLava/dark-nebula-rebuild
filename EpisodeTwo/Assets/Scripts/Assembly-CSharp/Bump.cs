using FLD;
using UnityEngine;

public class Bump : Entity
{
	private const float JUMP_INTERVAL = 0.66f;

	private static int _audioIndex;

	public AudioClip Sfx;

	private float _lastJump;

	private tk2dAnimatedSprite _sprite;

	private float originalRadius;

	public bool DontBump;

	private int _hasBeenHit;

	protected override void Awake()
	{
		base.Awake();
		_sprite = GetComponentInChildren<tk2dAnimatedSprite>();
		originalRadius = (base.GetComponent<Collider>() as CapsuleCollider).radius;
	}

	public void SetRadius(float radius)
	{
		(base.GetComponent<Collider>() as CapsuleCollider).radius = radius;
	}

	public override void OnTakenFromPool()
	{
		base.OnTakenFromPool();
		SetRadius(originalRadius);
		base.enabled = true;
	}

	public override void OnReturnedToPool()
	{
		base.OnReturnedToPool();
		base.enabled = false;
	}

	public bool HasBeenHit()
	{
		return _hasBeenHit > 0;
	}

	private void Update()
	{
		_hasBeenHit--;
	}

	private void OnEnable()
	{
		if (_sprite != null)
		{
			_sprite.enabled = true;
		}
	}

	private void OnDisable()
	{
		if (_sprite != null)
		{
			_sprite.enabled = false;
		}
	}

	private void OnTriggerStay(Collider other)
	{
		if (!DontBump && other.gameObject.CompareTag("Player") && !(_lastJump + 0.66f > Time.time))
		{
			_lastJump = Time.time;
			_hasBeenHit = 2;
			World.vessel.TriggerJump();
			GameHaptics.Play(HapticEvent.Spring);
			Audio.Play("bump" + _audioIndex, Sfx, World.vessel.transform.position);
			_audioIndex = (_audioIndex + 1) % 5;
			ObjectPool.instance.GetObjectForType<Shockwave>().Play(string.Empty, base.transform.position, 0.4f);
			ObjectPool.instance.GetObjectForType<Shockwave>().Play(string.Empty, base.transform.position);
			_sprite.Play("BumpJump");
			Invoke("PlayNormalAnimation", 0.66f);
		}
	}

	private void PlayNormalAnimation()
	{
		_sprite.Play("Bump");
	}
}

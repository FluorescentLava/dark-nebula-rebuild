using FLD;
using UnityEngine;

public class Bump : Entity
{
	private const float JUMP_INTERVAL = 0.66f;

	private static int _audioIndex;

	public AudioClip Sfx;

	private float _lastJump;

	private tk2dAnimatedSprite _sprite;

	private void Awake()
	{
		_sprite = GetComponentInChildren<tk2dAnimatedSprite>();
	}

	private void OnTriggerStay(Collider other)
	{
		if (other.gameObject.CompareTag("Player") && !(_lastJump + 0.66f > Time.time))
		{
			_lastJump = Time.time;
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

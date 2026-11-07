using UnityEngine;

public class AvatarButton : MonoBehaviour
{
	[SerializeField]
	private tk2dSprite _lockedIcon;

	[SerializeField]
	private Avatar _avatar;

	[SerializeField]
	private tk2dSprite _avatarIcon;

	private Vector3 _defaultScale = Vector3.one;

	public Avatar Avatar
	{
		get
		{
			return _avatar;
		}
		set
		{
			_avatar = value;
			_avatarIcon.SetSprite(_avatar.SpriteCollection, _avatar.IconID);
		}
	}

	private void Start()
	{
		_defaultScale = _avatarIcon.transform.localScale;
		UpdateLockStatus();
	}

	public void Bounce()
	{
		Go.to(_avatarIcon.transform, 0.02f, new GoTweenConfig().scale(_defaultScale + Vector3.one).setUpdateType(GoUpdateType.TimeScaleIndependentUpdate).onComplete(delegate
		{
			Go.to(_avatarIcon.transform, 0.3f, new GoTweenConfig().scale(_defaultScale).setEaseType(GoEaseType.BounceOut).setUpdateType(GoUpdateType.TimeScaleIndependentUpdate));
		}));
	}

	public void UpdateLockStatus()
	{
		_lockedIcon.gameObject.SetActive(!_avatar.Owned);
		if (_avatar.Owned)
		{
			_avatarIcon.color = Color.white;
		}
		else
		{
			_avatarIcon.color = Color.grey;
		}
	}
}

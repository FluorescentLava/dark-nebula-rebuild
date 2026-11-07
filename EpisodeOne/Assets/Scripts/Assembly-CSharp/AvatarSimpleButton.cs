using System.Collections;
using UnityEngine;

public class AvatarSimpleButton : SimpleButton
{
	[SerializeField]
	private tk2dSprite _avatar;

	private CameraFade fade;

	public override void Start()
	{
		fade = Object.FindObjectOfType<CameraFade>();
		base.Start();
		_avatar.SetSprite(AvatarManager.Instance.ActiveAvatar.SpriteCollection, AvatarManager.Instance.ActiveAvatar.IconID);
	}

	public override void OnButtonUp()
	{
		VesselSelectionScene.PreviousScene = Application.loadedLevelName;
		StartCoroutine(LoadAvatarShop());
		base.OnButtonUp();
	}

	private IEnumerator LoadAvatarShop()
	{
		fade = Object.FindObjectOfType<CameraFade>();
		if (fade != null)
		{
			fade.FadeOut();
			yield return new WaitForSeconds(0.5f);
		}
		Application.LoadLevel("AvatarShop");
	}
}

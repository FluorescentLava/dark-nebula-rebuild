using System;
using FLD;
using UnityEngine;

public class BoosterButton : SimpleButton
{
	[SerializeField]
	private GameObject _buyIcon;

	[SerializeField]
	private DynamicFont _nameText;

	[SerializeField]
	private DynamicFont _onOffText;

	[SerializeField]
	private tk2dSprite _iconLocked;

	[SerializeField]
	private tk2dSprite _iconEnabled;

	[SerializeField]
	private tk2dSprite _iconDisabled;

	[SerializeField]
	private Booster _booster;

	public Action<Booster> BuyClicked;

	private new void Start()
	{
		// The unlock badge has its own art below this control's root.
		LegacyControlTextLayout.Bind(_buyIcon.GetComponentInChildren<DynamicFont>(true), _buyIcon.GetComponent<Renderer>());
		_nameText.Text = _booster.LocalisedName;
		_iconLocked.SetSprite(_booster.SpriteCollection, _booster.LockedIconID);
		_iconEnabled.SetSprite(_booster.SpriteCollection, _booster.EnabledIconID);
		_iconDisabled.SetSprite(_booster.SpriteCollection, _booster.DisabledIconID);
		RefreshIcon();
		base.Start();
	}

	private void RefreshIcon()
	{
		bool flag = BoosterManager.Have(_booster);
		bool flag2 = BoosterManager.IsEnabled(_booster, LoadLevelScreen.LevelToLoad, UserSettings.Instance.IsExpertMode);
		_iconDisabled.gameObject.SetActive(flag && !flag2);
		_iconEnabled.gameObject.SetActive(flag2);
		_iconLocked.gameObject.SetActive(!flag);
		_buyIcon.SetActive(!flag);
		_onOffText.gameObject.SetActive(flag);
		_onOffText.Text = ((!flag2) ? "OFF".Localize() : "ON".Localize());
	}

	private void EnableBooster(bool pEnable)
	{
		if (pEnable)
		{
			BoosterManager.EnableBooster(_booster, LoadLevelScreen.LevelToLoad, UserSettings.Instance.IsExpertMode);
		}
		else
		{
			BoosterManager.DisableBooster(_booster, LoadLevelScreen.LevelToLoad, UserSettings.Instance.IsExpertMode);
		}
		RefreshIcon();
	}

	public override void OnButtonUp()
	{
		if (BoosterManager.Have(_booster))
		{
			EnableBooster(!BoosterManager.IsEnabled(_booster, LoadLevelScreen.LevelToLoad, UserSettings.Instance.IsExpertMode));
		}
		else if (BuyClicked != null)
		{
			BuyClicked(_booster);
		}
		RefreshIcon();
		base.OnButtonUp();
	}
}

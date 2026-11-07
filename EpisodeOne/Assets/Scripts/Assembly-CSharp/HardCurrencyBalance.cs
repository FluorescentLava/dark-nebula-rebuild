using System;
using UnityEngine;

public class HardCurrencyBalance : MonoBehaviour
{
	[SerializeField]
	private DynamicFont _balanceText;

	[SerializeField]
	private SimpleButton _addHCButton;

	[SerializeField]
	private Transform _background;

	private bool _hidden;

	private void Awake()
	{
		_addHCButton.gameObject.SetActive(false);
		_background.localPosition = new Vector3(_background.localPosition.x - 50f, _background.localPosition.y, _background.localPosition.z);
	}

	private void Start()
	{
		if (UserPurchases.Instance.HasUnlockAll)
		{
			HideAll();
			return;
		}
		HardCurrencyManager instance = HardCurrencyManager.Instance;
		instance.BalanceChangedEvent = (Action)Delegate.Combine(instance.BalanceChangedEvent, new Action(HandleBalanceChanged));
		HandleBalanceChanged();
	}

	private void HideAll()
	{
		_hidden = true;
		base.gameObject.SetActive(false);
	}

	private void HandleBalanceChanged()
	{
		_balanceText.Text = HardCurrencyManager.Instance.Amount.ToString();
	}

	private void OnDestroy()
	{
		if (!_hidden)
		{
			HardCurrencyManager instance = HardCurrencyManager.Instance;
			instance.BalanceChangedEvent = (Action)Delegate.Remove(instance.BalanceChangedEvent, new Action(HandleBalanceChanged));
		}
	}
}

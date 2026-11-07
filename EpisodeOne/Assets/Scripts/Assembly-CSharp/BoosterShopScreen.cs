using System.Collections;
using FLD;
using UnityEngine;

public class BoosterShopScreen : MonoBehaviour
{
	[SerializeField]
	private SimpleButton _purchaseButton;

	[SerializeField]
	private MenuHeader _menuHeader;

	[SerializeField]
	private tk2dSprite _icon;

	[SerializeField]
	private DynamicFont _description;

	[SerializeField]
	private DynamicFont _name;

	[SerializeField]
	private DynamicFont _price;

	[SerializeField]
	private Alert _alertScreen;

	private CameraFade fade;

	private bool _isEnabled = true;
	private int inputEnabledFrame;

	private string _previousScene = "BoosterSelection";

	private Product _currentProduct;

	private void Start()
	{
		DarkNebulaAdsManager.Instance.ShowAd("BoosterShop").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		_purchaseButton.Text = "BUY".Localize();
		_menuHeader.SetTitleID("PURCHASE HEADER");
		_alertScreen.gameObject.SetActive(false);
		_alertScreen.EventYES += HideInsufficientShards;
		_isEnabled = true;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_purchaseButton.ButtonUpEvent += HandlePurchaseEvent;
		_menuHeader.BackButton.ButtonUpEvent += HandleBackEvent;
		if (BoosterManager.Instance.BoosterToBuy != null)
		{
			SetupBooster(BoosterManager.Instance.BoosterToBuy);
		}
	}

	private void SetupBooster(Booster pBooster)
	{
		_icon.SetSprite(pBooster.SpriteCollection, pBooster.IconID);
		_description.Text = pBooster.LocalisedDescription;
		_name.Text = pBooster.LocalisedName;
		_price.Text = pBooster.Price.ToString();
		_currentProduct = pBooster;
	}

	private void ShowInsufficientShards()
	{
		_isEnabled = false;
		_purchaseButton.enabled = false;
		_menuHeader.BackButton.enabled = false;
		_alertScreen.SetAcknowledgementOnly();
		_alertScreen.gameObject.SetActive(true);
		_alertScreen.SetTitle("HC NEEDED");
		_alertScreen.SetBody("HC NEEDED TEXT");
	}

	private void HideInsufficientShards()
	{
		_alertScreen.gameObject.SetActive(false);
		_isEnabled = true;
		inputEnabledFrame = Time.frameCount;
		_purchaseButton.enabled = true;
		_menuHeader.BackButton.enabled = true;
	}

	private void NotifyFlurryOfCurrentBooster()
	{
		Booster booster = (Booster)_currentProduct;
		if (booster.Type == Booster.BoosterType.SPEED)
		{
			DarkNebulaMetrics.LevelUpgradeSpeed(LoadLevelScreen.LevelToLoad);
		}
		else if (booster.Type == Booster.BoosterType.SHIELD)
		{
			DarkNebulaMetrics.LevelUpgradeShield(LoadLevelScreen.LevelToLoad);
		}
		else if (booster.Type == Booster.BoosterType.MAGNET)
		{
			DarkNebulaMetrics.LevelUpgradeMagnet(LoadLevelScreen.LevelToLoad);
		}
	}

	private void HandlePurchaseEvent(SimpleButton pButton)
	{
		if (!_isEnabled) return;
		if (HardCurrencyManager.Instance.Spend(_currentProduct.Price))
		{
			NotifyFlurryOfCurrentBooster();
			DarkNebulaMetrics.LevelUpgrade(_currentProduct.ID, LoadLevelScreen.LevelToLoad);
			DarkNebulaMetrics.CurrencyPurchase(_currentProduct.ID);
			BoosterManager.UnlockBooster(_currentProduct.ID);
			StartCoroutine(GotoPreviousScene());
		}
		else
		{
			ShowInsufficientShards();
		}
	}

	private void HandleBackEvent(SimpleButton pButton)
	{
		if (!_isEnabled) return;
		StartCoroutine(GotoPreviousScene());
	}

	private void Update()
	{
		if (_isEnabled && Time.frameCount > inputEnabledFrame && Input.GetKeyUp(KeyCode.Escape))
		{
			StartCoroutine(GotoPreviousScene());
		}
	}

	private IEnumerator GotoPreviousScene()
	{
		_purchaseButton.ButtonUpEvent -= HandlePurchaseEvent;
		_menuHeader.BackButton.ButtonUpEvent -= HandleBackEvent;
		_isEnabled = false;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(_previousScene);
	}

	private void OnDestroy()
	{
		_purchaseButton.ButtonUpEvent -= HandlePurchaseEvent;
		_menuHeader.BackButton.ButtonUpEvent -= HandleBackEvent;
		_alertScreen.EventYES -= HideInsufficientShards;
	}
}

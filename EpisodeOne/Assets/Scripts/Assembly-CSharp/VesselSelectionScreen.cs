using System;
using System.Collections.Generic;
using UnityEngine;

// Both entry points instantiate this same authored page and use this controller.
public sealed class VesselSelectionScreen : MonoBehaviour
{
    [SerializeField] AvatarButton _avatarButtonPrefab;
    [SerializeField] SimpleButton _equipButton;
    [SerializeField] DynamicFont _price, _avatarName, _balanceText;
    [SerializeField] GameObject _priceInfo;
    [SerializeField] AudioClip _swapSound;
    [SerializeField] MenuHeader _menuHeader;
    [SerializeField] SwipeSelector _swiper;
    readonly List<AvatarButton> avatars = new List<AvatarButton>();
    Action onClose;
    Camera viewCamera;
    Avatar visible;
    bool closing;

    public static void Open(IngameOptionScreen owner)
    {
        Create(() => { if (owner != null) { owner.gameObject.SetActive(true); owner.SetEnabled(true); } });
        owner.gameObject.SetActive(false);
    }

    public static VesselSelectionScreen Create(Action closed)
    {
        var root = Instantiate(Resources.Load<GameObject>("VesselSelection"));
        root.name = "Vessel selection";
        var page = root.GetComponentInChildren<VesselSelectionScreen>(true);
        page.onClose = closed;
        return page;
    }

    void Start()
    {
        viewCamera = transform.root.GetComponentInChildren<Camera>();
        viewCamera.depth = 100;
        viewCamera.clearFlags = CameraClearFlags.SolidColor;
        _swiper.ViewCamera = viewCamera;
        LegacyControlTextLayout.Bind(_balanceText, _balanceText.transform.parent.Find("Shard icon").GetComponent<Renderer>());
        foreach (SimpleButton button in transform.root.GetComponentsInChildren<SimpleButton>(true))
        {
            button.viewCamera = viewCamera;
            button.targetObject = null;
            button.messageName = string.Empty;
        }
        _equipButton.ButtonUpEvent += PurchaseOrEquip;
        _menuHeader.BackButton.ButtonUpEvent += Back;
        _swiper.SelectedIndexChanged += OnSelectionChanged;
        foreach (int index in AvatarUnlocks.DisplayOrder())
        {
            var avatar = AvatarManager.Instance.AllAvatars.Find(a => a.ID == AvatarUnlocks.Ids[index]);
            var button = Instantiate(_avatarButtonPrefab, transform);
            button.gameObject.layer = gameObject.layer;
            foreach (Transform child in button.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = gameObject.layer;
            button.Avatar = avatar;
            avatars.Add(button);
            _swiper.Add(button.transform, new Vector3((avatars.Count - 1) * 200f, 0, 0));
        }
        visible = AvatarManager.Instance.ActiveAvatar;
        Reorder();
        HardCurrencyManager.Instance.BalanceChangedEvent += Refresh;
        GamePreferences.Changed += Refresh;
        Refresh();
    }

    void Reorder()
    {
        int[] order = AvatarUnlocks.DisplayOrder();
        avatars.Sort((a, b) => Array.IndexOf(order, AvatarUnlocks.IndexOf(a.Avatar.ID)).CompareTo(Array.IndexOf(order, AvatarUnlocks.IndexOf(b.Avatar.ID))));
        _swiper.Reorder(avatars.ConvertAll(a => a.transform).ToArray(), 200f, avatars.FindIndex(a => a.Avatar == visible));
    }

    void OnSelectionChanged(int index)
    {
        visible = avatars[index].Avatar;
        FLD.Audio.Play("vessel-swap" + Time.realtimeSinceStartup, _swapSound);
        Refresh();
    }

    public void Browse(int delta) { _swiper.SelectedIndex = Mathf.Clamp(_swiper.SelectedIndex + delta, 0, avatars.Count - 1); }

    void Refresh()
    {
        if (visible == null) return;
        _menuHeader.SetTitleID("SELECT VESSEL");
        _menuHeader.BackButton.Text = GameLocalization.Text("GO BACK");
        _avatarName.Text = GameLocalization.Text(visible.ID);
        _balanceText.Text = AvatarUnlocks.Balance.ToString();
        _price.Text = AvatarUnlocks.Price(visible.ID).ToString();
        _priceInfo.SetActive(!visible.Owned);
        _equipButton.Text = GameLocalization.Text(!visible.Owned ? "BUY" : visible == AvatarManager.Instance.ActiveAvatar ? "EQUIPPED" : "EQUIP");
        _equipButton.enabled = visible.Owned || AvatarUnlocks.Balance >= AvatarUnlocks.Price(visible.ID);
        foreach (var button in avatars) button.UpdateLockStatus();
    }

    void PurchaseOrEquip(SimpleButton source)
    {
        if (!visible.Owned && !AvatarUnlocks.TryPurchase(visible.ID)) return;
        AvatarManager.Instance.ActiveAvatar = visible;
        if (World.vessel != null) World.vessel.SetAvatar();
        Reorder();
        Refresh();
        avatars.Find(a => a.Avatar == visible).Bounce();
    }

    void Back(SimpleButton source) { Close(); }
    public void Close()
    {
        if (closing) return;
        closing = true;
        Destroy(transform.root.gameObject);
        if (onClose != null) onClose();
    }
    void Update() { if (Input.GetKeyUp(KeyCode.Escape)) Close(); }
    void OnDestroy()
    {
        HardCurrencyManager.Instance.BalanceChangedEvent -= Refresh;
        GamePreferences.Changed -= Refresh;
    }
}

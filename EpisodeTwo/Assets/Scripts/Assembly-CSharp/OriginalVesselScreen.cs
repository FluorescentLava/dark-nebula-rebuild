using System;
using UnityEngine;

// Extends the recovered settings controls and vessel artwork; no replacement UI skin.
public sealed class OriginalVesselScreen : MonoBehaviour
{
    IngameOptionScreen owner;
    FlipperButton selector;
    SimpleButton action, back;
    SimpleText details, balance;
    GameObject priceDisplay;
    tk2dSprite preview, lockedIcon;
    int[] order;
    int selected;
    bool closing;

    public static void Open(IngameOptionScreen source)
    {
        var root=new GameObject("Vessel selection");
        root.layer=source.gameObject.layer;
        root.transform.SetParent(source.transform.parent,false);
        root.transform.localPosition=source.transform.localPosition;
        root.transform.localRotation=source.transform.localRotation;
        root.transform.localScale=source.transform.localScale;
        var page=root.AddComponent<OriginalVesselScreen>();
        page.Initialize(source);
        source.gameObject.SetActive(false);
    }
    GameObject Copy(GameObject template,string name,float y)
    {
        GameObject copy=Instantiate(template,transform);
        copy.name=name; copy.SetActive(true);
        copy.transform.localPosition=new Vector3(0,y,template.transform.localPosition.z);
        foreach(SimpleButton button in copy.GetComponentsInChildren<SimpleButton>(true))
        { button.targetObject=null;button.messageName=string.Empty;button.enabled=true; }
        return copy;
    }
    void Initialize(IngameOptionScreen source)
    {
        owner=source;
        Transform sourceBackground=source.transform.Find("Background");
        if(sourceBackground!=null)
        {
            GameObject background=Instantiate(sourceBackground.gameObject,transform);
            background.name="Background";
            Vector3 position=background.transform.localPosition;
            position.z=10f;
            background.transform.localPosition=position;
            background.SetActive(true);
        }
        selected=VesselSkins.Selected;
        const float titleGap=8f;
        selector=Copy(source.FlipperMusic,"Vessel selector",150f-titleGap).GetComponent<FlipperButton>();
        selector.Title.transform.localPosition+=Vector3.up*titleGap;
        selector.EventChoiceChanged+=Choose;
        action=Copy(source.BtnCalibrateControls,"Purchase or equip",-145).GetComponent<SimpleButton>();
        back=Copy(source.BtnGoBack,"Back",-215).GetComponent<SimpleButton>();
        action.ButtonUpEvent+=PurchaseOrEquip;
        back.ButtonUpEvent+=Return;
        balance=CreateShardDisplay("Shard balance",new Vector3(-135,210,0),true);
        details=CreateShardDisplay("Shard price",new Vector3(0,-85,0),false);
        priceDisplay=details.transform.parent.gameObject;
        var art=new GameObject("Vessel preview");art.layer=source.gameObject.layer;
        art.transform.SetParent(transform,false);art.transform.localPosition=new Vector3(0,35,0);
        preview=tk2dSprite.AddComponent(art,VesselSkins.Collection(selected),24);
        GameObject lockArt=Instantiate(Resources.Load<GameObject>("VesselLock"),preview.transform,false);
        lockArt.name="LockedIcon";lockArt.layer=art.layer;
        lockedIcon=lockArt.GetComponent<tk2dSprite>();
        var fit=gameObject.AddComponent<MenuContentFit>();fit.Camera=back.viewCamera!=null?back.viewCamera:Camera.main;
        GamePreferences.Changed+=Refresh;
        ShardWallet.BalanceChanged+=RefreshDetails;
        Refresh();
    }
    SimpleText CreateShardDisplay(string name,Vector3 position,bool isBalance)
    {
        var root=new GameObject(name);root.layer=gameObject.layer;
        root.transform.SetParent(transform,false);root.transform.localPosition=position;
        var collection=Resources.Load<GameObject>("VesselLockArt/VesselLockCollection").GetComponent<tk2dSpriteCollectionData>();
        var icon=new GameObject("Shard icon");icon.layer=gameObject.layer;
        icon.transform.SetParent(root.transform,false);icon.transform.localPosition=new Vector3(isBalance?0:-40,0,0);
        var shard=tk2dSprite.AddComponent(icon,collection,collection.GetSpriteIdByName("HardCurrencyIcon"));
        shard.scale=new Vector3(.5f,.5f,1);
        var label=Instantiate(selector.Value,root.transform).GetComponent<SimpleText>();
        label.name="Amount";label.transform.localPosition=new Vector3(isBalance?28:15,0,-1);
        var mesh=label.GetComponent<tk2dTextMesh>();mesh.anchor=isBalance?TextAnchor.MiddleLeft:TextAnchor.MiddleCenter;
        mesh.color=new Color(0,1,0,1);
        return label;
    }
    void Choose(int position) { selected=order[position];RefreshDetails(); }
    public void Browse(int delta)
    {
        int position=Array.IndexOf(order,selected);
        selected=order[(position+delta+order.Length)%order.Length];Refresh();
    }
    void Refresh()
    {
        order=AvatarUnlocks.DisplayOrder();
        string[] names=new string[order.Length];
        for(int i=0;i<names.Length;i++)names[i]=AvatarUnlocks.Ids[order[i]];
        selector.InitChoices(Array.IndexOf(order,selected),names,"SELECT VESSEL");
        back.GetComponentInChildren<SimpleText>().SetStringID("GO BACK");
        RefreshDetails();
    }
    void RefreshDetails()
    {
        string id=AvatarUnlocks.Ids[selected];bool owned=AvatarUnlocks.IsUnlocked(id);
        balance.SetLiteralText(AvatarUnlocks.Balance.ToString());
        details.SetLiteralText(AvatarUnlocks.Price(id).ToString());
        priceDisplay.SetActive(!owned);
        string caption=!owned?"BUY":selected==VesselSkins.Selected?"EQUIPPED":"EQUIP";
        action.GetComponentInChildren<SimpleText>().SetStringID(caption);
        action.enabled=owned || AvatarUnlocks.Balance>=AvatarUnlocks.Price(id);
        tk2dSpriteCollectionData collection=VesselSkins.Collection(selected);
        preview.collection=collection;preview.spriteId=24;preview.Build();
        preview.color=owned?Color.white:Color.grey;
        lockedIcon.gameObject.SetActive(!owned);
        float size=collection.spriteDefinitions[24].boundsData[1].x;
        preview.transform.localScale=Vector3.one*(110f/Mathf.Max(.01f,size));
    }
    void PurchaseOrEquip(SimpleButton button)
    {
        string id=AvatarUnlocks.Ids[selected];
        if(!AvatarUnlocks.IsUnlocked(id) && !AvatarUnlocks.TryPurchase(id))return;
        VesselSkins.Selected=selected;
        StandardVessel player=World.vessel as StandardVessel;if(player!=null)player.RefreshSkin();
        Refresh();
    }
    void Return(SimpleButton button) { Close(); }
    public void Close()
    {
        if(closing)return;closing=true;
        if(owner!=null){owner.gameObject.SetActive(true);owner.SetEnabled(true);}
        Destroy(gameObject);
    }
    void Update() { if(Input.GetKeyUp(KeyCode.Escape))Close(); }
    void OnDestroy() { GamePreferences.Changed-=Refresh;ShardWallet.BalanceChanged-=RefreshDetails; }
}

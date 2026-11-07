using System.Collections.Generic;
using UnityEngine;

// Keep the authored panel/background in place and fit its controls into the
// same safe rectangle in both episodes, including transient loading panels.
public class MenuContentFit : MonoBehaviour
{
    public Camera Camera;
    readonly List<Transform> children=new List<Transform>();
    readonly List<Vector3> scales=new List<Vector3>(),positions=new List<Vector3>();
    readonly List<DynamicFont> fonts=new List<DynamicFont>();
    readonly List<System.Action> fontRefreshes=new List<System.Action>();
    bool initialized;
    bool refreshingFont;
    float lastSize=-1f,lastAspect=-1f;
    Rect lastSafe;
    int settleUntil;
    void OnEnable()
    {
        lastSize=-1f;settleUntil=Time.frameCount+15;
        if(initialized)
        {
            BindFonts();
            foreach(DynamicFont font in fonts)
                if(font!=null && font.transform.parent==transform)font.Refresh();
        }
    }
    void OnDisable(){UnbindFonts();}
    void OnDestroy(){UnbindFonts();}
    void BindFonts()
    {
        UnbindFonts();
        foreach(DynamicFont font in GetComponentsInChildren<DynamicFont>(true))
        {
            DynamicFont current=font;
            System.Action refresh=()=>FontRefreshed(current);
            fonts.Add(font);fontRefreshes.Add(refresh);font.FontRefreshed+=refresh;
        }
    }
    void UnbindFonts()
    {
        for(int i=0;i<fonts.Count;i++)if(fonts[i]!=null)fonts[i].FontRefreshed-=fontRefreshes[i];
        fonts.Clear();fontRefreshes.Clear();
    }
    void FontRefreshed(DynamicFont font)
    {
        if(refreshingFont)return;
        // DynamicFont restores its own authored position when its text changes.
        // Keep that new font offset as the base before fitting the panel again.
        int index=children.IndexOf(font.transform);
        if(index>=0)
        {
            Vector3 fittedScale=font.transform.localScale;
            if(fittedScale!=scales[index])
            {
                // Font offsets are measured in its current local scale. Rebuild
                // this direct label at its native scale before caching its base.
                refreshingFont=true;
                try
                {
                    font.transform.localScale=scales[index];
                    font.Refresh();
                    positions[index]=font.transform.localPosition;
                }
                finally{font.transform.localScale=fittedScale;refreshingFont=false;}
            }
            else positions[index]=font.transform.localPosition;
        }
        lastSize=-1f;settleUntil=Mathf.Max(settleUntil,Time.frameCount+1);
    }

    bool IsControl(Renderer r)
    {
        if(!r.enabled || r is ParticleSystemRenderer)return false;
        if(r.GetComponentInParent<SimpleButton>()!=null)return true;
        if(r.GetComponentInParent<tk2dTextMesh>()!=null)return true;
        if(r.name=="Logo" || r.name=="Title" || r.name=="Body" || r.name.StartsWith("Text"))return true;
        foreach(MonoBehaviour component in r.GetComponentsInParent<MonoBehaviour>())
            if(component!=null && (component.GetType().Name=="DynamicFont" || component.GetType().Name=="SimpleText"))return true;
        return false;
    }
    Rect BoundsOnScreen(Transform root=null)
    {
        Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        foreach(Renderer r in (root!=null?root:transform).GetComponentsInChildren<Renderer>())
        {
            if(!IsControl(r))continue;
            Bounds b=r.bounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 p=Camera.WorldToScreenPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
        }
        return float.IsInfinity(min.x)?new Rect():Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    void LateUpdate()
    {
        if(Camera==null)return;
        Rect safe=LegacyScreenFit.ContentRect();
        if(initialized && Mathf.Approximately(lastSize,Camera.orthographicSize) && Mathf.Approximately(lastAspect,Camera.aspect) && lastSafe==safe && Time.frameCount>settleUntil)return;
        if(!initialized)
        {
            foreach(Transform child in transform)
            {
                if(child.name=="Background" || child.name=="Header" || child.name=="Footer" || child.GetComponent<Camera>()!=null)continue;
                children.Add(child);scales.Add(child.localScale);positions.Add(child.localPosition);
            }
            initialized=true;
            BindFonts();
        }
        for(int i=0;i<children.Count;i++){children[i].localScale=scales[i];children[i].localPosition=positions[i];}
        Rect view=Camera.pixelRect;
        float margin=Screen.width*0.015f;
        safe=Rect.MinMaxRect(Mathf.Max(safe.xMin,view.xMin)+margin,Mathf.Max(safe.yMin,view.yMin)+margin,Mathf.Min(safe.xMax,view.xMax)-margin,Mathf.Min(safe.yMax,view.yMax)-margin);
        Rect bounds=BoundsOnScreen();
        if(bounds.width>0 && bounds.height>0)
        {
            float factor=Mathf.Min(1f,safe.width/bounds.width,safe.height/bounds.height);
            for(int i=0;i<children.Count;i++)
            {
                children[i].localScale=scales[i]*factor;
                Vector3 p=positions[i];p.x*=factor;p.y*=factor;children[i].localPosition=p;
            }
            bounds=BoundsOnScreen();
            var confirmation=GetComponent<ConfirmScreen>();
            bool center=confirmation!=null && GetComponentInParent<Hud>()!=null;
            if(center)
            {
                // Center the title bounds, including localized font offsets.
                Rect title=BoundsOnScreen(confirmation.lblTitle.transform);
                confirmation.lblTitle.transform.localPosition+=transform.InverseTransformVector(Camera.transform.right*(bounds.center.x-title.center.x)*(2f*Camera.orthographicSize/Camera.pixelHeight));
                bounds=BoundsOnScreen();
            }
            float dx=center?safe.center.x-bounds.center.x:bounds.xMin<safe.xMin?safe.xMin-bounds.xMin:bounds.xMax>safe.xMax?safe.xMax-bounds.xMax:0;
            float dy=center?safe.center.y-bounds.center.y:bounds.yMin<safe.yMin?safe.yMin-bounds.yMin:bounds.yMax>safe.yMax?safe.yMax-bounds.yMax:0;
            Vector3 offset=transform.InverseTransformVector((Camera.transform.right*dx+Camera.transform.up*dy)*(2f*Camera.orthographicSize/Camera.pixelHeight));
            foreach(Transform child in children)child.localPosition+=offset;
        }
        lastSize=Camera.orthographicSize;lastAspect=Camera.aspect;lastSafe=LegacyScreenFit.ContentRect();
    }
}

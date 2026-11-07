using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Menu artwork shares its original edge treatment across pages. Other backdrops
// retain their original cover or mirrored margins.
public class BackdropMargins : MonoBehaviour
{
    public Camera Camera;
    public bool CoverOriginal;
    readonly List<Mesh> meshes=new List<Mesh>();
    readonly List<GameObject> objects=new List<GameObject>();
    Renderer source;
    Vector3 nativeScale,nativePosition;
    float lastSize=-1f,lastAspect=-1f;
    IEnumerator Start()
    {
        yield return null;
        if(Camera==null)yield break;
        if(CoverOriginal && Camera.transform.parent!=null)
        {
            Transform background=Camera.transform.parent.Find("Background");
            if(background!=null)source=background.GetComponent<Renderer>();
        }
        float area=0;
        if(source==null)foreach(Renderer r in Object.FindObjectsByType<Renderer>())
        {
            if(r.gameObject.scene!=gameObject.scene || r.name!="Background" || r.GetComponent<MeshFilter>()==null)continue;
            float value=r.bounds.size.x*r.bounds.size.y;
            if(value>area){area=value;source=r;}
        }
        if(source==null)yield break;
        // Every page using the menu artwork shares the main menu's edge treatment.
        if(MenuBackdropExtension.TryApply(Camera,source)){enabled=false;yield break;}
        if(CoverOriginal)
        {
            nativeScale=source.transform.localScale;
            nativePosition=source.transform.localPosition;
            FitOriginal();yield break;
        }
        Create(source,true);Create(source,false);
    }
    void LateUpdate()
    {
        if(CoverOriginal && source!=null && Camera!=null &&
            (!Mathf.Approximately(lastSize,Camera.orthographicSize) || !Mathf.Approximately(lastAspect,Camera.aspect)))FitOriginal();
    }
    void FitOriginal()
    {
        LegacyScreenFit.CoverBackdrop(Camera,source,nativeScale,nativePosition);
        lastSize=Camera.orthographicSize;lastAspect=Camera.aspect;
    }
    void Create(Renderer source,bool top)
    {
        Mesh input=source.GetComponent<MeshFilter>().sharedMesh;
        if(input==null)return;
        Vector3 up=Camera.transform.up;
        Vector3 center=source.bounds.center;
        float edge=Vector3.Dot(center,up)+(top?1f:-1f)*source.bounds.extents.y;
        Vector3[] vertices=input.vertices;
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 p=source.transform.TransformPoint(vertices[i]);
            vertices[i]=p+2f*(edge-Vector3.Dot(p,up))*up;
        }
        int[] triangles=input.triangles;
        for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
        var mesh=new Mesh();mesh.name="Backdrop edge continuation";
        mesh.vertices=vertices;mesh.uv=input.uv;
        if(input.colors32.Length==input.vertexCount)mesh.colors32=input.colors32;
        mesh.triangles=triangles;mesh.RecalculateBounds();meshes.Add(mesh);
        var go=new GameObject(top?"Upper backdrop continuation":"Lower backdrop continuation");
        go.layer=source.gameObject.layer;go.transform.SetParent(transform.root,true);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial=source.sharedMaterial;objects.Add(go);
    }
    void OnDestroy()
    {
        foreach(GameObject go in objects)if(go!=null)Destroy(go);
        foreach(Mesh mesh in meshes)if(mesh!=null)Destroy(mesh);
    }
}

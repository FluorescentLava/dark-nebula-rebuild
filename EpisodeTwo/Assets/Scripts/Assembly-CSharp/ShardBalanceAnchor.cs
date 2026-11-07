using UnityEngine;

// Keep the frameless wallet in the safe top-left corner at every aspect ratio.
[DefaultExecutionOrder(2000)]
public sealed class ShardBalanceAnchor : MonoBehaviour
{
    public Camera ViewCamera;
    public Renderer Icon;
    void LateUpdate()
    {
        if (ViewCamera == null || Icon == null) return;
        Rect safe = LegacyScreenFit.ContentRect();
        Vector3 center = ViewCamera.WorldToScreenPoint(Icon.bounds.center);
        Vector3 edge = ViewCamera.WorldToScreenPoint(Icon.bounds.max);
        float margin = Mathf.Min(safe.width, safe.height) * .025f;
        Vector3 target = ViewCamera.ScreenToWorldPoint(new Vector3(safe.xMin + margin + Mathf.Abs(edge.x - center.x), safe.yMax - margin - Mathf.Abs(edge.y - center.y), center.z));
        transform.position += target - Icon.bounds.center;
    }
}

using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class HoverMoveStable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Movement Settings")]
    public float moveUpAmount = 10f;   // How much to move up (in UI units)
    public float smoothTime = 0.08f;   // Smaller = snappier motion
    public bool useSmoothDamp = true;

    private RectTransform rt;
    private Vector3 originalLocalPos;
    private Vector3 targetLocalPos;
    private Vector3 velocity = Vector3.zero;
    private bool isHovered = false;

    // Screen-space hover region captured at Start (based on resting position)
    private float originalTopY;
    private float originalBottomY;
    private float originalCenterY;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    void Start()
    {
        originalLocalPos = rt.localPosition;
        targetLocalPos = originalLocalPos;

        CaptureOriginalScreenRect();
    }

    void Update()
    {
        if (useSmoothDamp)
            rt.localPosition = Vector3.SmoothDamp(rt.localPosition, targetLocalPos, ref velocity, smoothTime);
        else
            rt.localPosition = Vector3.Lerp(rt.localPosition, targetLocalPos, Time.deltaTime * (1f / Mathf.Max(smoothTime, 0.0001f)));
    }

    // Capture the resting rect in screen space once, so movement won't affect the hover test
    private void CaptureOriginalScreenRect()
    {
        // find canvas camera (null is fine for ScreenSpaceOverlay)
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        Camera cam = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = rootCanvas.worldCamera;

        Vector3[] worldCorners = new Vector3[4];
        rt.GetWorldCorners(worldCorners);

        // convert corners to screen points and find top/bottom
        Vector2[] screenCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
            screenCorners[i] = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[i]);

        // corners: 0=bottom-left, 1=top-left, 2=top-right, 3=bottom-right
        float topY = Mathf.Max(screenCorners[1].y, screenCorners[2].y);
        float bottomY = Mathf.Min(screenCorners[0].y, screenCorners[3].y);

        originalTopY = topY;
        originalBottomY = bottomY;
        originalCenterY = (topY + bottomY) * 0.5f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EvaluatePointerForHover(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        EvaluatePointerForHover(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // pointer left the button entirely -> cancel hover
        if (isHovered)
        {
            isHovered = false;
            targetLocalPos = originalLocalPos;
        }
    }

    private void EvaluatePointerForHover(PointerEventData eventData)
    {
        // Use eventData.position (screen-space, pixels)
        float pointerY = eventData.position.y;

        bool pointerInTopHalfOfOriginal = pointerY > originalCenterY;

        if (pointerInTopHalfOfOriginal && !isHovered)
        {
            isHovered = true;
            targetLocalPos = originalLocalPos + Vector3.up * moveUpAmount;
        }
        else if (!pointerInTopHalfOfOriginal && isHovered)
        {
            isHovered = false;
            targetLocalPos = originalLocalPos;
        }
    }

    /// <summary>
    /// If your layout changes at runtime (ContentSizeFitter/LayoutGroup), call this to recapture the hover region.
    /// </summary>
    public void RefreshOriginalRegion()
    {
        CaptureOriginalScreenRect();
        originalLocalPos = rt.localPosition;
        targetLocalPos = originalLocalPos;
        velocity = Vector3.zero;
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh Original Region")]
    private void EditorRefresh() => RefreshOriginalRegion();
#endif
}

using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class HoverMoveStable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Movement Settings")]
    public float moveUpAmount = 10f;
    public float smoothTime = 0.08f;
    public bool useSmoothDamp = true;

    [Header("Tooltip Settings")]
    [TextArea(3, 10)]
    public string tooltipText;

    private RectTransform rt;
    private Vector3 originalLocalPos;
    private Vector3 targetLocalPos;
    private Vector3 velocity = Vector3.zero;
    private bool isHovered = false;

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

    private void CaptureOriginalScreenRect()
    {
        Canvas rootCanvas = GetComponentInParent<Canvas>();
        Camera cam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;

        Vector3[] worldCorners = new Vector3[4];
        rt.GetWorldCorners(worldCorners);

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[0]);
        Vector2 screenPoint2 = RectTransformUtility.WorldToScreenPoint(cam, worldCorners[1]);
        originalCenterY = (screenPoint.y + screenPoint2.y) * 0.5f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(tooltipText))
        {
            TooltipManager.Instance.ShowTooltip(tooltipText);
        }
        EvaluatePointerForHover(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        TooltipManager.Instance.UpdatePosition();
        EvaluatePointerForHover(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
        if (isHovered)
        {
            isHovered = false;
            targetLocalPos = originalLocalPos;
        }
    }

    private void EvaluatePointerForHover(PointerEventData eventData)
    {
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

    public void RefreshOriginalRegion()
    {
        CaptureOriginalScreenRect();
        originalLocalPos = rt.localPosition;
        targetLocalPos = originalLocalPos;
        velocity = Vector3.zero;
    }
}
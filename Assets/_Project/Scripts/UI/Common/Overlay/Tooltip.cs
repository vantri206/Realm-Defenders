using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class Tooltip : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private Canvas overlayCanvas;

    [Header("Position")]
    [SerializeField] private Vector2 pointerGapDistance = new Vector2(20f, 20f);

    [Header("Text Layout")]
    [SerializeField, Min(1f)] private float maxTextWidth = 480f;
    [SerializeField] private RectOffset textPadding;

    private RectTransform tooltipRect;
    private RectTransform textRect;
    private CanvasGroup canvasGroup;
    private ContentSizeFitter textSizeFitter;
    private LayoutElement textLayoutElement;

    private void Awake()
    {
        CacheReferences();

        if (textPadding == null)
        {
            textPadding = new RectOffset(16, 16, 8, 8);
        }

        tooltipRect = (RectTransform)transform;
        textRect = tooltipText.rectTransform;

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (tooltipText != null)
        {
            tooltipText.raycastTarget = false;
            tooltipText.textWrappingMode = TextWrappingModes.Normal;
            tooltipText.overflowMode = TextOverflowModes.Overflow;
        }

        if (textRect != null && textPadding != null)
        {
            Vector2 topLeft = new Vector2(0f, 1f);
            textRect.anchorMin = topLeft;
            textRect.anchorMax = topLeft;
            textRect.pivot = topLeft;
            textRect.anchoredPosition = new Vector2(textPadding.left, -textPadding.top);
        }

        if (textSizeFitter != null)
        {
            textSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            textSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    public void Show(string text, Vector2 screenPosition)
    {
        gameObject.SetActive(true);

        tooltipText.text = text;
        UpdateSize(text);
        LayoutRebuilder.ForceRebuildLayoutImmediate(textRect);
        UpdatePosition(screenPosition);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    internal void UpdatePosition(Vector2 screenPosition)
    {
        RectTransform canvasRect = (RectTransform)overlayCanvas.transform;
        RectTransform tooltipParent = (RectTransform)tooltipRect.parent;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, ResolveEventCamera(), out Vector2 pointerPosition);

        Vector2 canvasPivotPosition = ResolvePivotPosition(pointerPosition, canvasRect.rect, tooltipRect.rect.size, tooltipRect.pivot, pointerGapDistance);

        Vector3 parentLocalPosition = tooltipParent.InverseTransformPoint(canvasRect.TransformPoint(canvasPivotPosition));
        parentLocalPosition.z = tooltipRect.localPosition.z;
        tooltipRect.localPosition = parentLocalPosition;
    }

    internal bool ContainsScreenPosition(Vector2 screenPosition)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(tooltipRect, screenPosition, ResolveEventCamera());
    }

    private void UpdateSize(string text)
    {
        float naturalWidth = tooltipText.GetPreferredValues(text, Mathf.Infinity, Mathf.Infinity).x;
        float textWidth = Mathf.Ceil(Mathf.Min(naturalWidth, maxTextWidth));
        float textHeight = Mathf.Ceil(tooltipText.GetPreferredValues(text, textWidth, Mathf.Infinity).y);

        textLayoutElement.preferredWidth = textWidth;
        textLayoutElement.preferredHeight = textHeight;

        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + textPadding.horizontal);
        tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight + textPadding.vertical);
    }

    private Camera ResolveEventCamera()
    {
        return overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : overlayCanvas.worldCamera;
    }

    private static Vector2 ResolvePivotPosition(Vector2 pointerPosition, Rect canvasRect, Vector2 tooltipSize, Vector2 tooltipPivot, Vector2 gapDistance)
    {
        float left = pointerPosition.x + gapDistance.x;
        if (left + tooltipSize.x > canvasRect.xMax)
        {
            left = pointerPosition.x - gapDistance.x - tooltipSize.x;
        }

        float bottom = pointerPosition.y - gapDistance.y - tooltipSize.y;
        if (bottom < canvasRect.yMin)
        {
            bottom = pointerPosition.y + gapDistance.y;
        }

        left = ClampLowerEdge(left, canvasRect.xMin, canvasRect.xMax, tooltipSize.x);
        bottom = ClampLowerEdge(bottom, canvasRect.yMin, canvasRect.yMax, tooltipSize.y);

        return new Vector2(
            left + tooltipSize.x * tooltipPivot.x,
            bottom + tooltipSize.y * tooltipPivot.y);
    }

    private static float ClampLowerEdge(float lowerEdge, float boundsMin, float boundsMax, float size)
    {
        float maximumLowerEdge = boundsMax - size;
        if (maximumLowerEdge < boundsMin)
        {
            return boundsMin + (boundsMax - boundsMin - size) * 0.5f;
        }
        else
        {
            return Mathf.Clamp(lowerEdge, boundsMin, maximumLowerEdge);
        }
    }

    private void CacheReferences()
    {
        textLayoutElement = tooltipText.GetComponent<LayoutElement>();
        canvasGroup = GetComponent<CanvasGroup>();
        textSizeFitter = tooltipText.GetComponent<ContentSizeFitter>();
    }
}

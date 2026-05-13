using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public Canvas rootCanvas;
    [SerializeField] public Image sourceIconImage;

    private RectTransform dragVisual;
    private IItemSource source;
    private NoOfItems payload;

    private void Awake()
    {
        AutoWire();
    }

    private void OnEnable()
    {
        AutoWire();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        AutoWire();

        if (source == null) return;

        payload = source.TakeAll();
        if (payload.IsEmpty) return;

        if (!rootCanvas)
        {
            source.PutBack(payload);
            payload.Clear();
            return;
        }

        GameObject visual = new GameObject("DraggingIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragVisual = visual.GetComponent<RectTransform>();
        dragVisual.SetParent(rootCanvas.transform, false);
        dragVisual.SetAsLastSibling();

        RectTransform selfRect = transform as RectTransform;
        dragVisual.sizeDelta = selfRect != null ? selfRect.rect.size : new Vector2(64f, 64f);

        Image dragImage = visual.GetComponent<Image>();
        dragImage.raycastTarget = false;
        dragImage.preserveAspect = true;
        dragImage.sprite = payload.item != null ? payload.item.icon : null;

        CanvasGroup cg = visual.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.8f;

        MoveVisual(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveVisual(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!payload.IsEmpty && source != null)
        {
            source.PutBack(payload);
            payload.Clear();
        }

        if (dragVisual)
            Destroy(dragVisual.gameObject);
    }

    public NoOfItems TakePayload()
    {
        NoOfItems p = payload;
        payload.Clear();
        return p;
    }

    public void ReturnRemainder(NoOfItems remainder)
    {
        if (remainder.IsEmpty) return;
        if (source != null)
            source.PutBack(remainder);
    }

    private void MoveVisual(PointerEventData eventData)
    {
        if (!dragVisual || eventData == null) return;

        RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;
        if (canvasRect != null && rootCanvas.renderMode != RenderMode.WorldSpace)
        {
            Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, cam, out Vector2 localPoint);
            dragVisual.anchoredPosition = localPoint;
        }
        else
        {
            dragVisual.position = eventData.position;
        }
    }

    private void AutoWire()
    {
        if (!rootCanvas)
            rootCanvas = GetComponentInParent<Canvas>();

        if (!rootCanvas)
            rootCanvas = FindFirstObjectByType<Canvas>();

        if (!sourceIconImage)
            sourceIconImage = GetComponent<Image>();

        source = GetComponent<IItemSource>();
        if (source == null)
            source = GetComponentInParent<IItemSource>();
    }
}

public interface IItemSource
{
    NoOfItems TakeAll();
    void PutBack(NoOfItems stack);
}

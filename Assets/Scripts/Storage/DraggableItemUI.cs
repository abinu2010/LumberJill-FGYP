using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] public Canvas rootCanvas;      
    [SerializeField] public Image sourceIconImage;
    private RectTransform dragVisual;  
    private Image dragImage;
    private Transform originalParent;
    private IItemSource source;        
    private NoOfItems payload;        
    private void Awake()
    {
        if (!rootCanvas) rootCanvas = GetComponentInParent<Canvas>();
        if (!sourceIconImage) sourceIconImage = GetComponent<Image>();

        source = GetComponent<IItemSource>() ?? GetComponentInParent<IItemSource>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (source == null) return;
        payload = source.TakeAll();
        if (payload.IsEmpty) return;
        dragVisual = new GameObject("DraggingIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image)).GetComponent<RectTransform>();
        dragVisual.SetParent(rootCanvas.transform, false);
        dragVisual.sizeDelta = (transform as RectTransform).rect.size;

        dragImage = dragVisual.GetComponent<Image>();
        dragImage.raycastTarget = false;
        dragImage.sprite = payload.item.icon;

        var cg = dragVisual.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.8f;

        dragVisual.position = eventData.position;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual) dragVisual.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!payload.IsEmpty && source != null)
        {
            source.PutBack(payload);
            payload.Clear();
        }

        if (dragVisual) Destroy(dragVisual.gameObject);
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
        if (source != null) source.PutBack(remainder);
    }
}
public interface IItemSource
{
    NoOfItems TakeAll(); 
    void PutBack(NoOfItems stack); 
}
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StorageShelfSlot : MonoBehaviour, IItemSource, IDropHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI countText;

    private StorageManager storage;
    private IRebuildRequester owner;
    private ItemSO item;

    void Awake()
{
    storage = FindFirstObjectByType<StorageManager>();
    if (!icon) icon = transform.Find("Icon") ? transform.Find("Icon").GetComponent<Image>() : null;
    if (!nameText) nameText = transform.Find("Name") ? transform.Find("Name").GetComponent<TextMeshProUGUI>() : null;
    if (!countText) countText = transform.Find("Count") ? transform.Find("Count").GetComponent<TextMeshProUGUI>() : null;
    var bg = GetComponent<Image>();
    if (!bg)
    {
        bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0);
    }
    bg.raycastTarget = true;
}
    public void Bind(ItemSO i, IRebuildRequester gridOwner)
    {
        owner = gridOwner;
        item = i;

        if (item != null)
        {
            if (icon) { icon.enabled = true; icon.sprite = item.icon; if (!icon.GetComponent<DraggableItemUI>()) icon.gameObject.AddComponent<DraggableItemUI>(); }
            if (nameText) { nameText.gameObject.SetActive(true); nameText.text = item.displayName; }
            RefreshCount();
            enabled = true;
        }
        else ClearVisuals();
    }
    public void ClearVisuals()
    {
        item = null;
        if (icon) { icon.enabled = false; icon.sprite = null; }
        if (nameText) { nameText.text = ""; nameText.gameObject.SetActive(false); }
        if (countText) countText.text = "";
    }
    private void RefreshCount()
    {
        if (!countText || item == null || storage == null) return;
        countText.text = storage.GetCount(item).ToString();
    }
    public NoOfItems TakeAll()
    {
        if (item == null || storage == null) return default;
        int have = storage.GetCount(item);
        if (have <= 0) return default;

        int taken = storage.Take(item, have);
        RefreshCount();
        owner?.RequestRebuildSoon();
        return new NoOfItems { item = item, count = taken };
    }
    public void PutBack(NoOfItems stack)
    {
        if (stack.IsEmpty || storage == null) return;
        storage.Put(stack.item, stack.count);
        owner?.RequestRebuildSoon();
    }
    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItemUI>() : null;
        if (!drag) return;
        var payload = drag.TakePayload();
        if (payload.IsEmpty)
        {
            drag.ReturnRemainder(payload);
            return;
        }
        var grid = GetComponentInParent<StorageShelfGridAnchored>();
        if (grid && payload.item && payload.item.category != grid.category)
        {
            drag.ReturnRemainder(payload);
            return;
        }
        grid?.PinItemAt(this, payload.item);
        storage.Put(payload.item, payload.count);
        payload.Clear();
        owner?.RequestRebuildSoon();
        drag.ReturnRemainder(payload);

    }

}
public interface IRebuildRequester { void RequestRebuildSoon(); }

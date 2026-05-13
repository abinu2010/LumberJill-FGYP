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

    private void Awake()
    {
        storage = FindFirstObjectByType<StorageManager>();
        AutoWire();
    }

    private void OnEnable()
    {
        AutoWire();
    }

    public void Bind(ItemSO i, IRebuildRequester gridOwner)
    {
        owner = gridOwner;
        item = i;
        AutoWire();

        if (item != null)
        {
            if (icon)
            {
                icon.enabled = true;
                icon.sprite = item.icon;
                icon.raycastTarget = false;
            }

            if (nameText)
            {
                nameText.gameObject.SetActive(true);
                nameText.text = item.displayName;
                nameText.raycastTarget = false;
            }

            RefreshCount();
            enabled = true;
        }
        else
        {
            ClearVisuals();
        }
    }

    public void ClearVisuals()
    {
        item = null;

        if (icon)
        {
            icon.enabled = false;
            icon.sprite = null;
            icon.raycastTarget = false;
        }

        if (nameText)
        {
            nameText.text = string.Empty;
            nameText.gameObject.SetActive(false);
            nameText.raycastTarget = false;
        }

        if (countText)
        {
            countText.text = string.Empty;
            countText.raycastTarget = false;
        }
    }

    private void RefreshCount()
    {
        if (!countText || item == null || storage == null) return;
        countText.text = storage.GetCount(item).ToString();
        countText.raycastTarget = false;
    }

    public NoOfItems TakeAll()
    {
        if (item == null || storage == null) return default;

        int have = storage.GetCount(item);
        if (have <= 0) return default;

        ItemSO takenItem = item;
        int taken = storage.Take(item, have);
        RefreshCount();
        owner?.RequestRebuildSoon();

        return new NoOfItems { item = takenItem, count = taken };
    }

    public void PutBack(NoOfItems stack)
    {
        if (stack.IsEmpty) return;
        if (storage == null) storage = FindFirstObjectByType<StorageManager>();
        if (storage == null) return;

        storage.Put(stack.item, stack.count);
        owner?.RequestRebuildSoon();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;

        DraggableItemUI drag = eventData.pointerDrag.GetComponent<DraggableItemUI>();
        if (drag == null)
            drag = eventData.pointerDrag.GetComponentInParent<DraggableItemUI>();
        if (drag == null) return;

        NoOfItems payload = drag.TakePayload();
        if (payload.IsEmpty)
        {
            drag.ReturnRemainder(payload);
            return;
        }

        StorageShelfGridAnchored grid = GetComponentInParent<StorageShelfGridAnchored>();
        if (grid && payload.item && payload.item.category != grid.category)
        {
            drag.ReturnRemainder(payload);
            return;
        }

        if (storage == null)
            storage = FindFirstObjectByType<StorageManager>();

        if (storage == null)
        {
            drag.ReturnRemainder(payload);
            return;
        }

        if (grid != null)
            grid.PinItemAt(this, payload.item);

        storage.Put(payload.item, payload.count);
        payload.Clear();

        if (grid != null)
            grid.Rebuild();
        else
            owner?.RequestRebuildSoon();
    }

    private void AutoWire()
    {
        if (storage == null)
            storage = FindFirstObjectByType<StorageManager>();

        if (!icon)
        {
            Transform t = transform.Find("Icon");
            if (!t) t = transform.Find("ItemIcon");
            if (t) icon = t.GetComponent<Image>();
        }

        if (!nameText)
        {
            Transform t = transform.Find("Name");
            if (!t) t = transform.Find("NameText");
            if (t) nameText = t.GetComponent<TextMeshProUGUI>();
        }

        if (!countText)
        {
            Transform t = transform.Find("Count");
            if (!t) t = transform.Find("CountText");
            if (!t) t = transform.Find("Amount");
            if (t) countText = t.GetComponent<TextMeshProUGUI>();
        }

        Image bg = GetComponent<Image>();
        if (!bg)
        {
            bg = gameObject.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.001f);
        }
        bg.raycastTarget = true;

        DraggableItemUI drag = GetComponent<DraggableItemUI>();
        if (drag == null)
            drag = gameObject.AddComponent<DraggableItemUI>();

        if (drag.rootCanvas == null)
            drag.rootCanvas = GetComponentInParent<Canvas>();
    }
}

public interface IRebuildRequester
{
    void RequestRebuildSoon();
}

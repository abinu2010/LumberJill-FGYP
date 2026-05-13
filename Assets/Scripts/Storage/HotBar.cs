using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotBar : MonoBehaviour, IDropHandler, IItemSource
{
    [Header("UI refs")]
    [SerializeField] private RectTransform slotArea;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Capacity")]
    [Min(1)] public int slotCapacity = 20;

    [SerializeField] private NoOfItems stack;

    public NoOfItems Peek() => stack;
    public bool IsEmpty => stack.IsEmpty;

    private void Awake()
    {
        AutoWire();
        RefreshUI();
    }

    private void OnEnable()
    {
        AutoWire();
        RefreshUI();
    }

    private void Start()
    {
        AutoWire();
        Canvas.ForceUpdateCanvases();
        RefreshUI();
    }

    private void OnValidate()
    {
        if (!slotArea) slotArea = transform as RectTransform;
    }

    public void SetStack(NoOfItems s)
    {
        stack = s;
        ClampStack();
        RefreshUI();
    }

    public bool AddFrom(ref NoOfItems source)
    {
        if (source.IsEmpty) return false;

        AutoWire();

        int before = source.count;

        if (IsEmpty)
        {
            stack.item = source.item;
            int moved = Mathf.Min(GetMaxFor(source.item), source.count);
            stack.count = moved;
            source.count -= moved;
        }
        else if (stack.item == source.item)
        {
            int space = Mathf.Max(0, GetMaxFor(stack.item) - stack.count);
            int moved = Mathf.Min(space, source.count);
            stack.count += moved;
            source.count -= moved;
        }

        if (source.count <= 0)
            source.Clear();

        ClampStack();
        RefreshUI();

        return source.count < before;
    }

    public NoOfItems TakeAll()
    {
        NoOfItems taken = stack;
        stack.Clear();
        RefreshUI();
        return taken;
    }

    public void PutBack(NoOfItems returnedStack)
    {
        if (returnedStack.IsEmpty) return;

        NoOfItems remainder = returnedStack;
        AddFrom(ref remainder);

        if (!remainder.IsEmpty)
        {
            StorageManager storage = FindFirstObjectByType<StorageManager>();
            if (storage != null)
                storage.Put(remainder.item, remainder.count);
        }
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

        AddFrom(ref payload);
        drag.ReturnRemainder(payload);
    }

    private int GetMaxFor(ItemSO item)
    {
        if (item == null) return Mathf.Max(1, slotCapacity);
        return Mathf.Min(Mathf.Max(1, slotCapacity), Mathf.Max(1, item.maxStack));
    }

    private void ClampStack()
    {
        if (stack.IsEmpty)
        {
            stack.Clear();
            return;
        }

        stack.count = Mathf.Clamp(stack.count, 0, GetMaxFor(stack.item));
        if (stack.count <= 0)
            stack.Clear();
    }

    private void RefreshUI()
    {
        AutoWire();

        bool has = !stack.IsEmpty;

        if (icon != null)
        {
            icon.enabled = has;
            icon.sprite = has && stack.item != null ? stack.item.icon : null;
            icon.raycastTarget = false;
        }

        if (countText != null)
        {
            countText.text = has && stack.count > 1 ? stack.count.ToString() : string.Empty;
            countText.raycastTarget = false;
        }
    }

    private void AutoWire()
    {
        if (!slotArea)
            slotArea = transform as RectTransform;

        Image slotImage = GetComponent<Image>();
        if (slotImage == null)
        {
            slotImage = gameObject.AddComponent<Image>();
            slotImage.color = new Color(1f, 1f, 1f, 0.001f);
        }
        slotImage.raycastTarget = true;

        if (!icon)
        {
            Transform t = transform.Find("Icon");
            if (!t) t = transform.Find("ItemIcon");
            if (t) icon = t.GetComponent<Image>();
        }

        if (!icon)
            icon = CreateIcon();

        if (!countText)
        {
            Transform t = transform.Find("Count");
            if (!t) t = transform.Find("CountText");
            if (!t) t = transform.Find("Amount");
            if (!t) t = transform.Find("Text");
            if (t) countText = t.GetComponent<TextMeshProUGUI>();
        }

        if (!countText)
            countText = CreateCountText();

        DraggableItemUI drag = GetComponent<DraggableItemUI>();
        if (drag == null)
            drag = gameObject.AddComponent<DraggableItemUI>();

        if (drag.rootCanvas == null)
            drag.rootCanvas = GetComponentInParent<Canvas>();
    }

    private Image CreateIcon()
    {
        GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(48f, 48f);

        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.enabled = false;
        img.preserveAspect = true;
        return img;
    }

    private TextMeshProUGUI CreateCountText()
    {
        GameObject go = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-4f, 3f);
        rt.sizeDelta = new Vector2(50f, 24f);

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.BottomRight;
        text.fontSize = 18f;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }
}

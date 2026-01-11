using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HotBar : MonoBehaviour, IDropHandler
{

    [Header("UI refs")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Capacity")]
    [Min(1)] public int slotCapacity = 20; // how much this slot can hold

    [SerializeField] private NoOfItems stack;

    public NoOfItems Peek() => stack; // allow drag to read what we have
    public bool IsEmpty => stack.IsEmpty;

    
    public void SetStack(NoOfItems s)
    {
        stack = s;
        RefreshUI();
    }
    public void AddFrom(ref NoOfItems source)
    {
        if (source.IsEmpty) return;

        // If empty, we can accept the item type
        if (IsEmpty)
        {
            stack.item = source.item;
            int canTake = Mathf.Min(slotCapacity, stack.item.maxStack, source.count);
            stack.count = canTake;
            source.count -= canTake;
        }
        // If same item, try to top up
        else if (stack.item == source.item)
        {
            int maxHere = Mathf.Min(slotCapacity, stack.item.maxStack);
            int space = maxHere - stack.count;
            int moved = Mathf.Min(space, source.count);
            stack.count += moved;
            source.count -= moved;
        }

        RefreshUI();
    }
    public NoOfItems TakeAll()
    {
        NoOfItems taken = stack;
        stack.Clear();
        RefreshUI();
        return taken;
    }
    public void OnDrop(PointerEventData eventData)
    {
        var drag = eventData.pointerDrag ? eventData.pointerDrag.GetComponent<DraggableItemUI>() : null;
        if (drag == null) return;
        NoOfItems payload = drag.TakePayload();
        AddFrom(ref payload);
        drag.ReturnRemainder(payload);
    }
    private void RefreshUI()
    {
        bool has = !IsEmpty;
        icon.enabled = has;
        icon.sprite = has ? stack.item.icon : null;
        countText.text = has && stack.item.maxStack > 1 ? stack.count.ToString() : string.Empty;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Canvas.ForceUpdateCanvases();
        RefreshUI();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

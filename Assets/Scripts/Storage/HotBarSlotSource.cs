using UnityEngine;

public class HotbarSlotSource : MonoBehaviour, IItemSource
{
    private HotBar slot;

    private void Awake()
    {
        slot = GetComponent<HotBar>();
    }

    public NoOfItems TakeAll()
    {
        if (slot == null) return default;
        return slot.TakeAll();
    }

    public void PutBack(NoOfItems stack)
    {
        if (slot == null) return;
        slot.PutBack(stack);
    }
}

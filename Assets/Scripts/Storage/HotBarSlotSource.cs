using UnityEngine;
public class HotbarSlotSource : MonoBehaviour, IItemSource
{
    private HotBar slot;
    private void Awake() => slot = GetComponent<HotBar>();
    public NoOfItems TakeAll()
    {
        return slot.TakeAll();
    }
    public void PutBack(NoOfItems stack)
    {
        // dump back into slot respecting capacity
        slot.AddFrom(ref stack);
    }
}

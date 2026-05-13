using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StorageShelfGridAnchored : MonoBehaviour, IRebuildRequester
{
    public ItemCategory category;
    public StorageManager storageManager;
    public GameObject shelfSlotPrefab;
    public List<RectTransform> anchors = new List<RectTransform>();
    public bool stretchToAnchor = true;

    private readonly List<StorageShelfSlot> slots = new List<StorageShelfSlot>();
    private Coroutine rebuildCo;
    private List<ItemSO> order = new List<ItemSO>();
    private int pinIndex = -1;
    private ItemSO pinItem;

    private void OnEnable()
    {
        if (!storageManager) storageManager = FindFirstObjectByType<StorageManager>();
        BuildOrReuseSlots();
        Rebuild();
    }

    public void RequestRebuildSoon()
    {
        if (!isActiveAndEnabled) return;
        if (rebuildCo != null) StopCoroutine(rebuildCo);
        rebuildCo = StartCoroutine(RebuildNextFrame());
    }

    private IEnumerator RebuildNextFrame()
    {
        yield return null;
        rebuildCo = null;
        Rebuild();
    }

    private void BuildOrReuseSlots()
    {
        slots.Clear();

        for (int i = 0; i < anchors.Count; i++)
        {
            RectTransform anchor = anchors[i];
            if (!anchor) continue;

            StorageShelfSlot slot = anchor.GetComponentInChildren<StorageShelfSlot>(true);
            if (!slot)
            {
                if (!shelfSlotPrefab) continue;

                GameObject go = Instantiate(shelfSlotPrefab, anchor);
                go.name = "ShelfSlot_" + (i + 1).ToString();
                slot = go.GetComponent<StorageShelfSlot>();

                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (stretchToAnchor)
                    {
                        rt.anchorMin = Vector2.zero;
                        rt.anchorMax = Vector2.one;
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }
                    else
                    {
                        rt.anchorMin = new Vector2(0.5f, 0.5f);
                        rt.anchorMax = new Vector2(0.5f, 0.5f);
                        rt.anchoredPosition = Vector2.zero;
                    }
                }
            }

            if (slot != null)
                slots.Add(slot);
        }
    }

    public void PinItemAt(StorageShelfSlot slot, ItemSO item)
    {
        pinIndex = slots.IndexOf(slot);
        pinItem = item;
    }

    public void Rebuild()
    {
        if (storageManager == null) storageManager = FindFirstObjectByType<StorageManager>();
        if (slots.Count == 0) BuildOrReuseSlots();
        if (slots.Count == 0 || storageManager == null) return;

        List<ItemSO> available = storageManager.AllItems()
            .Where(kv => kv.Key && kv.Value > 0 && kv.Key.category == category)
            .Select(kv => kv.Key)
            .ToList();

        List<ItemSO> ordered = order.Where(it => it && available.Contains(it)).ToList();

        if (pinItem && available.Contains(pinItem))
        {
            ordered.Remove(pinItem);
            int idx = Mathf.Clamp(pinIndex, 0, Mathf.Min(slots.Count - 1, ordered.Count));
            ordered.Insert(idx, pinItem);
        }

        for (int i = 0; i < available.Count; i++)
        {
            ItemSO item = available[i];
            if (!ordered.Contains(item))
                ordered.Add(item);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            if (i < ordered.Count)
                slots[i].Bind(ordered[i], this);
            else
                slots[i].ClearVisuals();
        }

        order = ordered;
        pinIndex = -1;
        pinItem = null;
    }
}

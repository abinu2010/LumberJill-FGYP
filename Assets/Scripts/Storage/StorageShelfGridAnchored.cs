using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StorageShelfGridAnchored : MonoBehaviour, IRebuildRequester
{
    [Header("Page")]
    public ItemCategory category;            
    public StorageManager storageManager;
    public GameObject shelfSlotPrefab;
    public List<RectTransform> anchors = new(); 
    public bool stretchToAnchor = true;

    private readonly List<StorageShelfSlot> slots = new();
    private Coroutine rebuildCo;

    private List<ItemSO> _order = new();
    private int _pinIndex = -1;
    private ItemSO _pinItem = null;
    void OnEnable()
    {
        if (!storageManager) storageManager = FindFirstObjectByType<StorageManager>();
        BuildOrReuseSlots();
        Rebuild();
    }
    public void RequestRebuildSoon()
    {
        if (rebuildCo != null) return;
        rebuildCo = StartCoroutine(RebuildNextFrame());
    }
    private IEnumerator RebuildNextFrame() { yield return null; rebuildCo = null; Rebuild(); }
    private void BuildOrReuseSlots()
    {
        slots.Clear();

        for (int i = 0; i < anchors.Count; i++)
        {
            var anchor = anchors[i];
            if (!anchor) { Debug.LogWarning($"[StorageShelfGrid] Missing anchor {i}"); continue; }

            var slot = anchor.GetComponentInChildren<StorageShelfSlot>(true);
            if (!slot)
            {
                var go = Object.Instantiate(shelfSlotPrefab, anchor);
                go.name = $"ShelfSlot_{i + 1}";
                slot = go.GetComponent<StorageShelfSlot>();
                var rt = go.GetComponent<RectTransform>();
                if (stretchToAnchor)
                {
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                }
                else
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = Vector2.zero;
                }
            }
            slots.Add(slot);
        }
    }
    public void PinItemAt(StorageShelfSlot slot, ItemSO item)
    {
        _pinIndex = slots.IndexOf(slot);
        _pinItem = item;
    }
    public void Rebuild()
    {
        if (slots.Count == 0 || storageManager == null) return;
        var available = storageManager
            .AllItems()
            .Where(kv => kv.Key && kv.Value > 0 && kv.Key.category == category)
            .Select(kv => kv.Key)
            .ToList();
        var ordered = _order.Where(it => it && available.Contains(it)).ToList();
        if (_pinItem && available.Contains(_pinItem))
        {
            ordered.Remove(_pinItem);
            int idx = Mathf.Clamp(_pinIndex, 0, Mathf.Min(slots.Count - 1, ordered.Count));
            ordered.Insert(idx, _pinItem);
        }
        foreach (var it in available) if (!ordered.Contains(it)) ordered.Add(it);
        for (int i = 0; i < slots.Count; i++)
        {
            if (i < ordered.Count) slots[i].Bind(ordered[i], this);
            else slots[i].ClearVisuals();
        }
        _order = ordered;
        _pinIndex = -1;
        _pinItem = null;
    }

}

using System.Collections.Generic;
using UnityEngine;
public class StorageManager : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public ItemSO item;
        public int count;
    }

    public List<Entry> startingItems = new();

    private Dictionary<ItemSO, int> stock = new();

    private void Awake()
    {
        foreach (var e in startingItems)
        {
            if (!e.item) continue;

            string key = "Storage_" + e.item.id;
            if (PlayerPrefs.HasKey(key)) continue; 

            if (!stock.ContainsKey(e.item)) stock[e.item] = 0;
            stock[e.item] += Mathf.Max(0, e.count);
        }

    }

    public int GetCount(ItemSO item) => item && stock.TryGetValue(item, out var c) ? c : 0;
    public int Take(ItemSO item, int amount)
    {
        if (!item || amount <= 0) return 0;
        if (!stock.TryGetValue(item, out var have) || have <= 0) return 0;

        int give = Mathf.Min(have, amount);
        stock[item] = have - give;
        return give;
    }
    public void SetCount(ItemSO item, int amount)
    {
        if (!item) return;
        stock[item] = Mathf.Max(0, amount);
    }
    public void ClearAll()
    {
        stock.Clear(); // wipe all items from memory
        PlayerPrefs.Save();
   }
    // Put amount back
    public void Put(ItemSO item, int amount)
    {
        if (!item || amount <= 0) return;
        if (!stock.ContainsKey(item)) stock[item] = 0;
        stock[item] += amount;
    }
    public IEnumerable<KeyValuePair<ItemSO, int>> AllItems() => stock;
}


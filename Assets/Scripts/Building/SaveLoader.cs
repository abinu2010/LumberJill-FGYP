using UnityEngine;

public class SaveLoader : MonoBehaviour
{
    [Header("References")]
    [SerializeField] public Inventory inventory;
    [SerializeField] public StorageManager storage;
    [SerializeField] public BuildingSystem buildingSystem;
    [SerializeField] public ShopItemSO[] shopItems;

    void Awake()
    {
        if (storage == null)
            storage = FindFirstObjectByType<StorageManager>();

        if (buildingSystem == null)
            buildingSystem = FindFirstObjectByType<BuildingSystem>();

        if (inventory == null)
        {
            GameObject gm = GameObject.FindWithTag("GameController");
            if (gm != null)
                inventory = gm.GetComponent<Inventory>();
        }

        LoadInventory();
        LoadStorage();
    }

    void Start()
    {
        LoadPlacedObjects();
    }

    void OnApplicationQuit()
    {
        SaveInventory();
        SaveStorage();
    }

    void SaveInventory()
    {
        if (inventory == null) return;
        PlayerPrefs.SetFloat("Money", inventory.money);
        PlayerPrefs.SetInt("Xp", inventory.xp);
        PlayerPrefs.SetInt("Lumber", inventory.lumber);
        PlayerPrefs.Save();
    }

    void LoadInventory()
    {
        if (inventory == null) return;
        inventory.money = PlayerPrefs.GetFloat("Money", 5000f);
        inventory.AddXp(PlayerPrefs.GetInt("Xp", 0));
        inventory.lumber = PlayerPrefs.GetInt("Lumber", 0);
        inventory.RefreshUI();
    }

    void SaveStorage()
    {
        if (storage == null) return;

        foreach (var kv in storage.AllItems())
        {
            if (kv.Key == null) continue;
            PlayerPrefs.SetInt("Storage_" + kv.Key.id, kv.Value);
        }
        PlayerPrefs.Save();
    }

    void LoadStorage()
    {
        if (storage == null) return;

        foreach (var e in storage.startingItems)
        {
            if (!e.item) continue;
            int saved = PlayerPrefs.GetInt("Storage_" + e.item.id, e.count);
            storage.SetCount(e.item, saved);
        }
    }

    void LoadPlacedObjects()
    {
        if (buildingSystem == null || buildingSystem.gridLayout == null)
        {
            Debug.LogError("[SaveLoader] BuildingSystem not ready, cannot load placed objects.");
            return;
        }

        if (shopItems == null || shopItems.Length == 0)
        {
            Debug.LogError(" no item");
            return;
        }

        foreach (var item in shopItems)
        {
            if (item == null) continue;
            if (item.prefabToPlace == null) continue;
            if (string.IsNullOrEmpty(item.id)) continue;

            if (PlayerPrefs.GetInt("MachineOwned_" + item.id, 0) != 1)
                continue;

            Vector3 pos = new Vector3(
                PlayerPrefs.GetFloat("MachinePosX_" + item.id, 0f),
                PlayerPrefs.GetFloat("MachinePosY_" + item.id, 0f),
                PlayerPrefs.GetFloat("MachinePosZ_" + item.id, 0f)
            );

            Quaternion rot = Quaternion.Euler(
                0f,
                PlayerPrefs.GetFloat("MachineRotY_" + item.id, 0f),
                0f
            );

            GameObject obj = Instantiate(item.prefabToPlace, pos, rot);

            var p = obj.GetComponentInChildren<Placeble>();
            if (p == null)
            {
                continue;
            }

            p.prefabId = item.id;
            p.Load();
            p.ForceRefreshFootprint();

            Vector3Int start = buildingSystem.gridLayout.WorldToCell(p.GetStartPosition());
            buildingSystem.TakeArea(start, p.Size);

        }
    }
}

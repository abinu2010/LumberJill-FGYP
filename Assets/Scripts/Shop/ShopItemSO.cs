using UnityEngine;

public enum ShopItemType
{
    BuyItemToStorage,   // utilities / bulk items
    BuyMachineToPlace,  // machines with Placeble
    BuyFieldToPlace,    // tree fields with Placeble
    BuyRecipe           // unlock a ProductionRecipeSO
}

[CreateAssetMenu(menuName = "Shop/Shop Item", fileName = "ShopItem_")]
public class ShopItemSO : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // must be unique per shop item
    public string displayName;
    public Sprite icon;

    [Header("Price")]
    [Min(0)] public int price = 10;

    [Header("Type")]
    public ShopItemType type = ShopItemType.BuyItemToStorage;

    [Header("Item Settings")]
    public ItemSO item;               // used when type = BuyItemToStorage
    [Min(1)] public int itemCount = 1;

    [Header("Prefab Settings")]
    public GameObject prefabToPlace;  // used when type = BuyMachineToPlace / BuyFieldToPlace

    [Header("Recipe Settings")]
    public ProductionRecipeSO recipeToUnlock; // used when type = BuyRecipe

    [Header("Purchase Rules")]
    public bool singlePurchase = false;       // if true you can only buy this once
}

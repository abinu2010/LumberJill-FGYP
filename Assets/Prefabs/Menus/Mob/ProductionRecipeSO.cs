using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Production/Recipe", fileName = "Recipe_")]
public class ProductionRecipeSO : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string displayName;

    [Header("Output")]
    public ItemSO finishedProduct;
    public Blueprint blueprint;

    [Header("Slot Requirements")]
    public List<SlotRequirement> slots = new List<SlotRequirement>();

    [System.Serializable]
    public class SlotRequirement
    {
        public string slotId;
        public string label;
        [Min(1)] public int requiredWidth = 1;
        [Min(1)] public int requiredHeight = 1;
    }

    [Header("Unlock Settings")]
    public bool unlockedByDefault = false;
    [Min(0)] public int unlockCost = 100;

    [Header("UI Hint")]
    public Sprite hintSprite;
}

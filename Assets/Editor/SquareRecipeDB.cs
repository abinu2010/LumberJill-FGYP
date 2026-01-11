using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SquareRecipes", menuName = "WoodWorks/Square Recipe DB")]
public class SquareRecipeDB : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public int width;        // canonical width
        public int height;       // canonical height
        public ItemSO item;      // output item asset
        public int planksCost;   // stored plank cost
        public float seconds;    // stored craft time
    }
    public List<Entry> entries = new();
}

using System.Collections.Generic;
using UnityEngine;

public class RecipeUnlockManager : MonoBehaviour
{
    public static RecipeUnlockManager Instance { get; private set; }

    [Header("Unlocked by default")]
    public List<ProductionRecipeSO> defaultUnlocked = new List<ProductionRecipeSO>();

    readonly HashSet<string> unlockedIds = new HashSet<string>();
    const string PrefPrefix = "RecipeUnlocked_";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadState();
    }

    void LoadState()
    {
        unlockedIds.Clear();

        if (defaultUnlocked != null)
        {
            for (int i = 0; i < defaultUnlocked.Count; i++)
            {
                var recipe = defaultUnlocked[i];
                if (recipe == null || string.IsNullOrEmpty(recipe.id)) continue;

                unlockedIds.Add(recipe.id);
            }
        }

        foreach (var recipe in defaultUnlocked)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.id)) continue;

            int flag = PlayerPrefs.GetInt(PrefPrefix + recipe.id, 0);
            if (flag == 1)
                unlockedIds.Add(recipe.id);
        }

        Debug.Log("[RecipeUnlockManager] Start unlocked count = " + unlockedIds.Count);
    }

    public bool IsUnlocked(ProductionRecipeSO recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.id)) return false;

        if (unlockedIds.Contains(recipe.id)) return true;

        int flag = PlayerPrefs.GetInt(PrefPrefix + recipe.id, 0);
        if (flag == 1)
        {
            unlockedIds.Add(recipe.id);
            return true;
        }

        return false;
    }

    public void UnlockRecipe(ProductionRecipeSO recipe)
    {
        if (recipe == null || string.IsNullOrEmpty(recipe.id)) return;

        unlockedIds.Add(recipe.id);
        PlayerPrefs.SetInt(PrefPrefix + recipe.id, 1);
        PlayerPrefs.Save();

    }

    public List<ProductionRecipeSO> FilterUnlocked(IList<ProductionRecipeSO> all)
    {
        List<ProductionRecipeSO> result = new List<ProductionRecipeSO>();

        if (all == null) return result;

        for (int i = 0; i < all.Count; i++)
        {
            var r = all[i];
            if (r == null) continue;

            if (IsUnlocked(r))
                result.Add(r);
        }

        return result;
    }
}

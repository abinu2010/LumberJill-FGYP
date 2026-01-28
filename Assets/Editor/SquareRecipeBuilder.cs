#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class SquareRecipeBuilder
{
    const int MAX_DIM = 8;
    const bool UNIQUE_PAIRS = true;
    const int PLANKS_PER_WIDTH = 1;
    const float SECONDS_FOR_2x2 = 3f;

    const string DB_PATH = "Assets/Game/Databases/SquareRecipes.asset";
    const string ITEMS_FOLDER = "Assets/Game/Items/Squares";
    const string ICONS_FOLDER = "Assets/Game/Icons/Squares";

    [InitializeOnLoadMethod]
    static void AutoBuildOnLoad() { BuildDatabase(); }

    [MenuItem("Tools/Squares/Rebuild Database")]
    public static void BuildDatabase()
    {
        EnsureFolders();
        var db = LoadOrCreateDb();
        db.entries.Clear();

        int madeItems = 0;

        for (int w = 1; w <= MAX_DIM; w++)
        {
            int hStart = UNIQUE_PAIRS ? w : 1;
            for (int h = hStart; h <= MAX_DIM; h++)
            {
                int a = Mathf.Min(w, h);
                int b = Mathf.Max(w, h);

                var item = LoadOrCreateItem(a, b, ref madeItems);
                var sprite = LoadSpriteIfExists(a, b);

                bool itemDirty = false;
                if (sprite && item.icon != sprite) { item.icon = sprite; itemDirty = true; }
                if (itemDirty) EditorUtility.SetDirty(item);

                int cost = Mathf.Max(1, PLANKS_PER_WIDTH * a);
                float secs = SECONDS_FOR_2x2 * (Mathf.Max(1, a * b) / 4f);

                db.entries.Add(new SquareRecipeDB.Entry
                {
                    width = a,
                    height = b,
                    item = item,
                    planksCost = cost,
                    seconds = secs
                });
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/Squares/Populate Scene Cutters")]
    public static void PopulateCutters()
    {
        var db = AssetDatabase.LoadAssetAtPath<SquareRecipeDB>(DB_PATH);
        if (!db) return;

        var cutters = Object.FindObjectsByType<SquareCutter>(FindObjectsSortMode.None);
        foreach (var sc in cutters)
        {
            Undo.RecordObject(sc, "Populate Recipes");
            sc.recipes.Clear();

            foreach (var e in db.entries)
            {
                sc.recipes.Add(new SquareCutter.Recipe
                {
                    width = e.width,
                    height = e.height,
                    outputItem = e.item,
                    planksCost = e.planksCost,
                    seconds = e.seconds
                });
            }

            EditorUtility.SetDirty(sc);
        }

        AssetDatabase.SaveAssets();
    }

    static void EnsureFolders()
    {
        CreateIfMissing("Assets/Game");
        CreateIfMissing("Assets/Game/Databases");
        CreateIfMissing(ITEMS_FOLDER);
        CreateIfMissing(ICONS_FOLDER);
    }

    static void CreateIfMissing(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace("\\", "/");
        var name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }

    static SquareRecipeDB LoadOrCreateDb()
    {
        var db = AssetDatabase.LoadAssetAtPath<SquareRecipeDB>(DB_PATH);
        if (!db)
        {
            db = ScriptableObject.CreateInstance<SquareRecipeDB>();
            AssetDatabase.CreateAsset(db, DB_PATH);
        }
        return db;
    }

    static ItemSO LoadOrCreateItem(int a, int b, ref int made)
    {
        string path = $"{ITEMS_FOLDER}/Square_{a}x{b}.asset";
        var item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);

        if (!item)
        {
            item = ScriptableObject.CreateInstance<ItemSO>();
            AssetDatabase.CreateAsset(item, path);
            made++;
        }

        bool dirty = false;

        string expectedId = $"square_{a}x{b}";
        string expectedName = $"Square {a}x{b}";

        if (item.id != expectedId) { item.id = expectedId; dirty = true; }
        if (item.displayName != expectedName) { item.displayName = expectedName; dirty = true; }
        if (item.category != ItemCategory.Utility) { item.category = ItemCategory.Utility; dirty = true; }
        if (item.maxStack != 20) { item.maxStack = 20; dirty = true; }

        if (item.gridWidth != a) { item.gridWidth = a; dirty = true; }
        if (item.gridHeight != b) { item.gridHeight = b; dirty = true; }
        if (!item.isProductionSquarePiece) { item.isProductionSquarePiece = true; dirty = true; }

        if (dirty) EditorUtility.SetDirty(item);

        return item;
    }

    static Sprite LoadSpriteIfExists(int a, int b)
    {
        string iconPath = $"{ICONS_FOLDER}/Square_{a}x{b}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite) return sprite;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (!tex) return null;

        var assets = AssetDatabase.LoadAllAssetsAtPath(iconPath).OfType<Sprite>().ToArray();
        return assets.FirstOrDefault();
    }
}
#endif

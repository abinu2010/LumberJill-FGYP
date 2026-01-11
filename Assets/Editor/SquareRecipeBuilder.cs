#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

public static class SquareRecipeBuilder
{
    // tuning knobs
    const int MAX_DIM = 8;                     // max size supported
    const bool UNIQUE_PAIRS = true;            // 2x3 equals 3x2
    const int PLANKS_PER_WIDTH = 1;            // cost equals width
    const float SECONDS_FOR_2x2 = 3f;          // baseline craft time

    // project paths
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
        int linkedIcons = 0;
        int missingIcons = 0;

        for (int w = 1; w <= MAX_DIM; w++)
        {
            int hStart = UNIQUE_PAIRS ? w : 1;
            for (int h = hStart; h <= MAX_DIM; h++)
            {
                int a = Mathf.Min(w, h);
                int b = Mathf.Max(w, h);

                var item = LoadOrCreateItem(a, b, ref madeItems);
                var sprite = LoadSpriteIfExists(a, b);

                if (sprite)
                {
                    if (item.icon != sprite) item.icon = sprite;
                    linkedIcons++;
                    //Debug.Log($"[Squares] Icon linked -> Square_{a}x{b} :: {AssetDatabase.GetAssetPath(sprite)}");
                }
                else
                {
                    missingIcons++;
                    //Debug.LogWarning($"[Squares] Icon missing -> Square_{a}x{b} expected {ICONS_FOLDER}/Square_{a}x{b}.png");
                }

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
        //Debug.Log($"[Squares] DB rebuilt :: entries={db.entries.Count} itemsMade={madeItems} iconsLinked={linkedIcons} iconsMissing={missingIcons}");
    }

    [MenuItem("Tools/Squares/Populate Scene Cutters")]
    public static void PopulateCutters()
    {
        var db = AssetDatabase.LoadAssetAtPath<SquareRecipeDB>(DB_PATH);
        if (!db) { Debug.LogWarning("[Squares] Database missing."); return; }

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
            //Debug.Log($"[Squares] Cutter populated -> {sc.name} :: recipes={sc.recipes.Count}");
        }

        AssetDatabase.SaveAssets();
        //Debug.Log($"[Squares] Scene cutters populated :: count={cutters.Length}");
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
            item.id = $"square_{a}x{b}";
            item.displayName = $"Square {a}�{b}";
            item.category = ItemCategory.Utility;
            item.maxStack = 20;
            AssetDatabase.CreateAsset(item, path);
            made++;
            Debug.Log($"[Squares] Item created -> {path}");
        }

        EditorUtility.SetDirty(item);
        return item;
    }

    static Sprite LoadSpriteIfExists(int a, int b)
    {
        string iconPath = $"{ICONS_FOLDER}/Square_{a}x{b}.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        if (sprite) return sprite;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (!tex) return null;

        var assets = AssetDatabase.LoadAllAssetsAtPath(iconPath)
                                  .OfType<Sprite>()
                                  .ToArray();
        return assets.FirstOrDefault();
    }
}
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SquareCutter))]
public class SquareCutterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Auto-Fill Recipes with 1x1..8x8"))
            AutoFill((SquareCutter)target);
    }

    private void AutoFill(SquareCutter sc)
    {
        Undo.RecordObject(sc, "Auto-fill recipes");
        sc.recipes.Clear();

        for (int w = 1; w <= 8; w++)
            for (int h = 1; h <= 8; h++)
            {
                var item = FindPiece(w, h);
                if (!item) continue;

                var r = new SquareCutter.Recipe
                {
                    width = w,
                    height = h,
                    outputItem = item,
                    planksCost = 0,   // 0 = use sc.planksPerPiece
                    seconds = 0f      // 0 = use area-scaled time (3x3 = sc.secondsForThreeByThree)
                };
                sc.recipes.Add(r);
            }

        EditorUtility.SetDirty(sc);
        Debug.Log($"[SquareCutter] Filled {sc.recipes.Count} recipes.");
    }

    private ItemSO FindPiece(int w, int h)
    {
        // Try name first
        var guids = AssetDatabase.FindAssets($"t:ItemSO Square_{w}x{h}");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var it = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (it) return it;
        }
        // Fallback: look by id = "square_wxh"
        foreach (var g in AssetDatabase.FindAssets("t:ItemSO"))
        {
            var it = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(g));
            if (it && it.id == $"square_{w}x{h}") return it;
        }
        return null;
    }
}
#endif

using UnityEngine;

public static class PieceUtils
{
    // Returns true and outputs width/height if item.id == "square_wxh"
    public static bool TryGetDims(ItemSO item, out int w, out int h)
    {
        w = h = 1;
        if (!item || string.IsNullOrEmpty(item.id)) return false;

        var s = item.id.ToLowerInvariant();   // e.g., "square_4x2"
        if (!s.StartsWith("square_")) return false;
        var parts = s.Substring(7).Split('x');
        if (parts.Length != 2) return false;

        return int.TryParse(parts[0], out w) && int.TryParse(parts[1], out h);
    }

    public static int Area(ItemSO item)
    {
        return TryGetDims(item, out int w, out int h) ? w * h : 1;
    }
}

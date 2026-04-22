namespace MATSING.Utils;

/// <summary>
/// Static utility providing a generic Fisher-Yates shuffle.
/// </summary>
public static class ShuffleHelper
{
    private static readonly Random _rng = new();

    /// <summary>Returns a new list containing the same elements in random order.</summary>
    public static List<T> Shuffle<T>(List<T> source)
    {
        var list = new List<T>(source);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
        return list;
    }
}

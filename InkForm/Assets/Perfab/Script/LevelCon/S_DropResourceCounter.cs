using System;
using System.Collections.Generic;

public static class S_DropResourceCounter
{
    private const string DefaultResourceId = "block_fragment";
    private static readonly Dictionary<string, int> resources = new Dictionary<string, int>();

    public static event Action<string, int, int> OnResourceChanged;

    public static void Add(string resourceId, int amount)
    {
        if (amount <= 0)
            return;

        string key = NormalizeResourceId(resourceId);
        int total = Get(key) + amount;
        resources[key] = total;

        OnResourceChanged?.Invoke(key, amount, total);
    }

    public static int Get(string resourceId)
    {
        string key = NormalizeResourceId(resourceId);
        return resources.TryGetValue(key, out int amount) ? amount : 0;
    }

    public static void Reset(string resourceId)
    {
        string key = NormalizeResourceId(resourceId);
        if (!resources.Remove(key))
            return;

        OnResourceChanged?.Invoke(key, 0, 0);
    }

    public static void ResetAll()
    {
        if (resources.Count == 0)
            return;

        resources.Clear();
        OnResourceChanged?.Invoke(string.Empty, 0, 0);
    }

    private static string NormalizeResourceId(string resourceId)
    {
        return string.IsNullOrWhiteSpace(resourceId) ? DefaultResourceId : resourceId.Trim();
    }
}

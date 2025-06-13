using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Semi.Avalonia;

namespace AmxxTutorial.Shared;

public static class IconFactory
{
    private static readonly IResourceDictionary Resources = new Icons();
    private static readonly Dictionary<string, IconItem> Icons = new();

    public static async Task InitializeResourcesAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (Resources is null) return;

            foreach (var provider in Resources.MergedDictionaries)
            {
                if (provider is not ResourceDictionary dic) continue;

                foreach (var key in dic.Keys)
                {
                    string keyStr = key.ToString();
                    if (dic[key] is not Geometry geometry) continue;

                    Icons[keyStr.ToLowerInvariant()] = new IconItem
                    {
                        ResourceKey = keyStr,
                        Geometry = geometry
                    };
                }
            }
        });
    }
    public static Geometry? GetIcon(string key)
    {
        var lower = key.ToLowerInvariant();

        if (Icons.TryGetValue(lower, out var icon))
            return icon.Geometry;

        return null;
    }

    public static ObservableCollection<IconItem> GetIconsByKeys(IEnumerable<string> keys)
    {
        var result = new ObservableCollection<IconItem>();

        foreach (var key in keys)
        {
            var lower = key.ToLowerInvariant();

            if (Icons.TryGetValue(lower, out var icon))
                result.Add(icon);
        }

        return result;
    }
}


public class IconItem
{
    public string? ResourceKey { get; set; }
    public Geometry? Geometry { get; set; }
}
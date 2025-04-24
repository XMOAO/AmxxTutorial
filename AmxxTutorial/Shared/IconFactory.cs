using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Semi.Avalonia;

namespace AmxxTutorial.Shared;

public static class IconFactory
{
    private static readonly IResourceDictionary _resources = new Icons();

    private static readonly HashSet<string> _requiredFilledIcons = new() 
    { "SemiIconHome", "SemiIconArticle", "SemiIconFile", "SemiIconSearch", "SemiIconSetting", "SemiIconList" };

    private static readonly HashSet<string> _requiredStrokedIcons = new() 
    { "SemiIconBookOpenStroked", "SemiIconBookmarkAddStroked", "SemiIconBookmarkDeleteStroked" };

    private static readonly Dictionary<string, IconItem> _filledIcons = new();
    private static readonly Dictionary<string, IconItem> _strokedIcons = new();

    public static bool IsInitialized { get; private set; }

    public static void InitializeResources()
    {
        if (_resources is null) return;

        Dispatcher.UIThread.Invoke(() =>
        {
            foreach (var provider in _resources.MergedDictionaries)
            {
                if (provider is not ResourceDictionary dic) continue;

                foreach (var key in dic.Keys)
                {
                    string keyStr = key.ToString();
                    if (dic[key] is not Geometry geometry) continue;

                    if (_requiredFilledIcons.Contains(keyStr))
                    {
                        _filledIcons[keyStr.ToLowerInvariant()] = new IconItem
                        {
                            ResourceKey = keyStr,
                            Geometry = geometry
                        };
                    }
                    else if (_requiredStrokedIcons.Contains(keyStr))
                    {
                        _strokedIcons[keyStr.ToLowerInvariant()] = new IconItem
                        {
                            ResourceKey = keyStr,
                            Geometry = geometry
                        };
                    }
                }
            }
        });
    }
    public static Geometry? GetIcon(string key)
    {
        var lower = key.ToLowerInvariant();
        if (_filledIcons.TryGetValue(lower, out var filled))
            return filled.Geometry;
        if (_strokedIcons.TryGetValue(lower, out var stroked))
            return stroked.Geometry;
        return null;
    }

    public static ObservableCollection<IconItem> GetIconsByKeys(IEnumerable<string> keys)
    {
        var result = new ObservableCollection<IconItem>();

        foreach (var key in keys)
        {
            var lower = key.ToLowerInvariant();

            if (_filledIcons.TryGetValue(lower, out var filled))
            {
                result.Add(filled);
            }
            else if (_strokedIcons.TryGetValue(lower, out var stroked))
            {
                result.Add(stroked);
            }
        }

        return result;
    }
}


public class IconItem
{
    public string? ResourceKey { get; set; }
    public Geometry? Geometry { get; set; }
}
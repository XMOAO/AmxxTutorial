using System;
using System.Collections.Generic;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Markup.Xaml;

using AmxxTutorial.Resources.Locales;
using Semi.Avalonia.Locale;

namespace AmxxTutorial.Shared;

public class ResLocalization : Styles
{
    private static readonly Dictionary<CultureInfo, ResourceDictionary> _localeToResource = new()
    {
        { new CultureInfo("zh-cn"), new Locales_zh_cn() },
        { new CultureInfo("en-us"), new Locales_en_us() },
        { new CultureInfo("zh-tw"), new Locales_zh_tw() },
    };

    private static readonly ResourceDictionary _defaultResource = new zh_cn();
    private static ResourceDictionary? _currentGlobalLocale; // For App.Resources
    private CultureInfo? _locale;

    public static event EventHandler? LocaleChanged;

    public ResLocalization(IServiceProvider? provider = null)
    {
        AvaloniaXamlLoader.Load(provider, this);
    }

    public CultureInfo? Locale
    {
        get => _locale;
        set
        {
            if (TryGetLocaleResource(value, out var newResource))
            {
                _locale = value;
                // 替换自身的 Resources 内容
                this.Resources.Clear();
                foreach (var kv in newResource)
                    this.Resources.Add(kv.Key, kv.Value);
            }
        }
    }

    private static bool TryGetLocaleResource(CultureInfo? locale, out ResourceDictionary resource)
    {
        if (locale == null || locale.Equals(CultureInfo.InvariantCulture))
        {
            resource = _defaultResource;
            return false;
        }

        if (_localeToResource.TryGetValue(locale, out resource!))
        {
            return true;
        }

        resource = _defaultResource;
        return false;
    }

    public static void OverrideLocaleResources(Application application, CultureInfo? culture)
    {
        if (application == null) return;

        if (!TryGetLocaleResource(culture, out var newDict))
            return;

        // 替换整个 ResourceDictionary
        if (_currentGlobalLocale != null)
            application.Resources.MergedDictionaries.Remove(_currentGlobalLocale);

        application.Resources.MergedDictionaries.Add(newDict);
        _currentGlobalLocale = newDict;

        // **通知所有订阅者**
        LocaleChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void OverrideLocaleResources(StyledElement element, CultureInfo? culture)
    {
        if (element == null) return;

        if (!TryGetLocaleResource(culture, out var newDict))
            return;

        // 如果原来有旧的语言包，可以手动移除（你也可以不处理）
        element.Resources.MergedDictionaries.Clear();
        element.Resources.MergedDictionaries.Add(newDict);
    }
}
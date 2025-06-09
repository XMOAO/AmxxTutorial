using Avalonia;
using Avalonia.Controls;
using System.ComponentModel;

namespace AmxxTutorial.Shared
{
    public static class Localization
    {
        public static object GetResource(string key, IResourceHost scope = null)
        {
            if (scope != null && scope.TryGetResource(key, out var value))
            {
                return value;
            }

            if (Application.Current.TryFindResource(key, out value))
            {
                return value;
            }

            return null;
        }
        public static string GetString(string key, IResourceHost scope = null, string defaultValue = null)
        {
            return GetResource(key, scope) as string ?? defaultValue;
        }
    }
    public class DescriptionLocalization : DescriptionAttribute
    {
        private readonly string _resourceKey;

        public DescriptionLocalization(string resourceKey)
        {
            _resourceKey = resourceKey;
        }

        public override string Description
        {
            get
            {
                // 尝试从资源字典获取值，找不到时直接返回 resourceKey
                if (Application.Current?.TryFindResource(_resourceKey, out var value) ?? false)
                {
                    return value as string ?? _resourceKey;
                }
                return _resourceKey;
            }
        }
    }
}

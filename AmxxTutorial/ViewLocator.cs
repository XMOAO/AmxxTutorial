using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using System.Reflection;
using System.Linq;
using AmxxTutorial.ViewModels;
using System.Collections.Generic;

namespace AmxxTutorial;

public class ViewLocator : IDataTemplate
{
    private Dictionary<Type, Control> PageCaches = new Dictionary<Type, Control>();

    public Control Build(object? data)
    {
        if (data is null)
        {
            return new TextBlock { Text = "data was null" };
        }

        var name = data.GetType().FullName!.Replace("ViewModel", "Page");
        var type = Type.GetType(name);

        if (type != null)
        {
            if (PageCaches.TryGetValue(type, out var control) && control != null)
                return PageCaches[type];
            else
            {
                PageCaches[type] = (Control)Activator.CreateInstance(type)!;
                return PageCaches[type];
            }
        }
        else
        {
            return new TextBlock { Text = "Not Found: " + name };
        }
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
using AmxxTutorial.ViewModels;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmxxTutorial.Shared
{
    public static class Globals
    {
        private static MainViewModel? MainViewModel { get; set; }

        public static void Init(MainViewModel vm)
        {
            MainViewModel ??= vm;
        }

        public static bool GetCurTheme() => MainViewModel.GetCurTheme();

        public static event EventHandler<bool>? OnThemeChanged
        {
            add => MainViewModel!.OnThemeChanged += value;
            remove => MainViewModel!.OnThemeChanged -= value;
        }
    }
}

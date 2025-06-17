using AmxxTutorial.Shared;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmxxTutorial.ViewModels
{
    public partial class MainSplashViewModel : ViewModelBase, IDialogContext
    {
        [ObservableProperty] private double _LoadingProgress;
        [ObservableProperty] private string _LoadingTip;

        private readonly TaskCompletionSource ReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public MainViewModel ViewModel;

        public MainSplashViewModel()
        {
            ViewModel = new MainViewModel();
            _ = Init();
        }

        public void SignalReady() => ReadyTcs.TrySetResult();

        public async Task Init()
        {
            await ReadyTcs.Task;

            var progressSteps = new List<(Func<Task>? Task, int Weight, string Str)>
            {
                (IncReader.InitializeIncFilesAsync, 30, Localization.GetString("LoadingInc")),
                (null, 15, ""),
                (null, 15, ""),
                (null, 5, ""),
                (IconFactory.InitializeResourcesAsync, 10, Localization.GetString("LoadingIcon")),
                (null, 5, ""),
                (ViewModel.InitializePageAndMenu, 30, Localization.GetString("LoadingInterface")),
                (null, 10, ""),
            };

            int totalWeight = progressSteps.Sum(s => s.Weight);
            int currentProgress = 0;

            foreach (var step in progressSteps)
            {
                if (step.Task is not null)
                    await step.Task();
                else
                    await Task.Delay(new Random().Next(200, 200 + (step.Weight * 10)));

                currentProgress += step.Weight;
                LoadingProgress = (int)((double)currentProgress / totalWeight * 100);

                if (!string.IsNullOrEmpty(step.Str))
                    LoadingTip = step.Str;
            }

            await Task.Delay(100);
            RequestClose?.Invoke(this, true);
        }

        public void Close()
        {
            RequestClose?.Invoke(this, false);
        }
        public event EventHandler<object?>? RequestClose;
    }
}

using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using PinballApi;

namespace Ifpa.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        protected readonly ILogger logger;

        [ObservableProperty]
        bool isBusy = false;

        [ObservableProperty]
        string title = string.Empty;

        protected BaseViewModel(ILogger<BaseViewModel> logger)
        {
            this.logger = logger;
        }
    }
}

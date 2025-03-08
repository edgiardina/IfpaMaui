using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

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

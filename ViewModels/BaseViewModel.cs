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
        public PinballRankingApiV2 PinballRankingApiV2 { get; private set; }

        protected readonly ILogger logger;

        [ObservableProperty]
        bool isBusy = false;

        [ObservableProperty]
        string title = string.Empty;

        public BaseViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<BaseViewModel> logger)
        {
            PinballRankingApiV2 = pinballRankingApiV2;
            this.logger = logger;
        }

        protected BaseViewModel(ILogger<BaseViewModel> logger)
        {
            this.logger = logger;
        }
    }
}

using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Ifpa.ViewModels
{
    public partial class PlayerDetailNoPlayerSelectedViewModel : BaseViewModel
    {
        private readonly IDispatcher dispatcher;

        public PlayerDetailNoPlayerSelectedViewModel(IDispatcher dispatcher, ILogger<PlayerDetailNoPlayerSelectedViewModel> logger) : base(logger)
        {
            this.dispatcher = dispatcher;
        }

        [RelayCommand]
        public async Task NavigateToSearch()
        {
            await dispatcher.DispatchAsync(async () =>
            {
                await Shell.Current.GoToAsync("///rankings?showSearch=true");
            });
        }
    }
}

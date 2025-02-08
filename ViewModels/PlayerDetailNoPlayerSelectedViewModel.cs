using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;

namespace Ifpa.ViewModels
{
    public partial class PlayerDetailNoPlayerSelectedViewModel : BaseViewModel
    {

        public PlayerDetailNoPlayerSelectedViewModel(ILogger<PlayerDetailNoPlayerSelectedViewModel> logger) : base(logger)
        {

        }

        [RelayCommand]
        public async Task NavigateToSearch()
        {
            await Shell.Current.GoToAsync("///rankings/player-search");
        }
    }
}

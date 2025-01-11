using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;

namespace Ifpa.ViewModels
{
    public partial class PlayerDetailNoPlayerSelectedViewModel : BaseViewModel
    {

        public PlayerDetailNoPlayerSelectedViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerDetailNoPlayerSelectedViewModel> logger) : base(pinballRankingApiV2, logger)
        {

        }

        [RelayCommand]
        public async Task NavigateToSearch()
        {
            await Shell.Current.GoToAsync("///rankings/player-search");
        }
    }
}

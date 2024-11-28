using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ifpa.ViewModels
{
    public partial class TournamentSearchViewModel : ObservableObject
    {
        private readonly IPinballRankingApi pinballRankingApi;
        private readonly ILogger<TournamentSearchViewModel> logger;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private List<Tournament> tournaments;

        [ObservableProperty]
        private Tournament selectedTournament;

        [ObservableProperty]
        private string searchTerm;

        [RelayCommand]
        public async Task TournamentSearch()
        {
            if (!IsBusy)
            {
                IsBusy = true;
                var search = await pinballRankingApi.TournamentSearch(name: string.IsNullOrEmpty(SearchTerm) ? null : SearchTerm,
                                                                      tournamentSearchSortMode: TournamentSearchSortMode.StartDate, 
                                                                      tournamentSearchSortOrder: TournamentSearchSortOrder.Descending, 
                                                                      onlyWithResults:true);
                Tournaments = search.Tournaments.Where(n => n.Winner.PlayerId.HasValue).ToList();
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task TournamentSelected()
        {
            await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedTournament.TournamentId}");
        }

        public TournamentSearchViewModel(IPinballRankingApi pinballRankingApi, ILogger<TournamentSearchViewModel> logger)
        {
            this.pinballRankingApi = pinballRankingApi;
            this.logger = logger;
        }

    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Tournaments.Related;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    [QueryProperty("TournamentId", "tournamentId")]
    public partial class RelatedTournamentsViewModel : BaseViewModel
    {
        public ObservableCollection<RelatedTournament> RelatedTournaments { get; set; }

        public int TournamentId { get; set; }

        [ObservableProperty]
        private RelatedTournament selectedTournament;

        private readonly IPinballRankingApi PinballRankingApi;

        public RelatedTournamentsViewModel(IPinballRankingApi pinballRankingApi, ILogger<RelatedTournamentsViewModel> logger) : base(logger)
        {
            Title = "Related Tournaments";
            RelatedTournaments = new ObservableCollection<RelatedTournament>();
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                RelatedTournaments.Clear();
                
                // Call the related tournaments API endpoint
                var relatedTournaments = await PinballRankingApi.GetRelatedTournaments(TournamentId);

                foreach (var tournament in relatedTournaments)
                {
                    RelatedTournaments.Add(tournament);
                }

                OnPropertyChanged(nameof(RelatedTournaments));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading related tournaments for tournament {0}", TournamentId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task TournamentSelected()
        {
            if (SelectedTournament == null)
                return;

            // Check if tournament has results (is completed) or is upcoming
            // If the tournament has a winner with a player ID, it's completed and has results
            if (SelectedTournament.Winner is not null)
            {
                // Tournament is completed, navigate to tournament results
                await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedTournament.TournamentId}");
            }
            else
            {
                // Tournament is upcoming, navigate to calendar detail
                await Shell.Current.GoToAsync($"calendar-detail?tournamentId={SelectedTournament.TournamentId}");
            }

            SelectedTournament = null;
        }
    }
}
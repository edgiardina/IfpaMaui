using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Tournaments;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class TournamentResultsViewModel : BaseViewModel
    {
        public ObservableCollection<TournamentResult> Results { get; set; }

        public Tournament TournamentDetails { get; set; }        

        public int TournamentId { get; set; }

        [ObservableProperty]
        private TournamentResult selectedPlayer;

        private readonly IPinballRankingApi PinballRankingApi;

        public TournamentResultsViewModel(IPinballRankingApi pinballRankingApi, ILogger<TournamentResultsViewModel> logger) : base(logger)
        {
            Title = "Tournament Results";
            Results = new ObservableCollection<TournamentResult>();
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
                Results.Clear();
                var tournamentResults = await PinballRankingApi.GetTournamentResults(TournamentId);
                TournamentDetails = await PinballRankingApi.GetTournament(TournamentId);

                Results = tournamentResults.Results.ToObservableCollection();

                Title = TournamentDetails.TournamentName;
                OnPropertyChanged(nameof(TournamentDetails));
                OnPropertyChanged(nameof(Results));

                AddTournamentToAppLinks();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading tournament results id {0}", TournamentId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ShareTournament()
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Uri = $"https://www.ifpapinball.com/tournaments/view.php?t={TournamentId}",
                Title = "Share Tournament Results"
            });
        }

        [RelayCommand]
        public async Task ViewTournamentInfo()
        {
            await Shell.Current.GoToAsync($"tournament-info?tournamentId={TournamentId}");
        }

        [RelayCommand]
        public async Task ViewPlayerDetails()
        {
            await Shell.Current.GoToAsync($"player-details?playerId={SelectedPlayer.PlayerId.Value}");
            SelectedPlayer = null;
        }

        private void AddTournamentToAppLinks()
        {
            var url = $"https://www.ifpapinball.com/tournaments/view.php?t={TournamentId}";

            var entry = new AppLinkEntry
            {
                Title = TournamentDetails.TournamentName,
                Description = TournamentDetails.EventName,
                AppLinkUri = new Uri(url, UriKind.RelativeOrAbsolute),
                IsLinkActive = true
                //No thumbnail for Tournament Results
            };

            entry.KeyValues.Add("contentType", "Tournament Result");
            entry.KeyValues.Add("appName", "IFPA Companion");
            try
            {
                Application.Current.AppLinks.RegisterLink(entry);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Error registering app link {0}", entry);
            }
        }
    }
}
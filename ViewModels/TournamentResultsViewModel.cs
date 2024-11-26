using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Tournaments;
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

        public TournamentResultsViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<TournamentResultsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Tournament Results";
            Results = new ObservableCollection<TournamentResult>();
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
                var tournamentResults = await PinballRankingApiV2.GetTournamentResults(TournamentId);
                TournamentDetails = await PinballRankingApiV2.GetTournament(TournamentId);

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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Players;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class PlayerVersusPlayerViewModel : BaseViewModel
    {
        public ObservableCollection<PlayerVersusRecordGroup> AllResults { get; set; }


        [ObservableProperty]
        private PlayerVersusRecord selectedPvp;


        [RelayCommand]
        public async Task ShowPvpDetail()
        {
            await Shell.Current.GoToAsync($"pvp-detail?playerId={PlayerId}&comparePlayerId={SelectedPvp.PlayerId}");
            SelectedPvp = null;
        }

        [RelayCommand]
        private async Task ShowPvpFilter()
        {
            string action = await Shell.Current.DisplayActionSheet("PVP Type", null, null, "All", "Top 250");

            if (action == "All")
            {
                await LoadAllItems();
            }
            else
            {
                await LoadEliteItems();
            }
        }

        public int PlayerId { get; set; }

        private readonly IPinballRankingApi PinballRankingApi;

        [ObservableProperty]
        private bool hasNoPvpData;

        public PlayerVersusPlayerViewModel(IPinballRankingApi pinballRankingApi, ILogger<PlayerVersusPlayerViewModel> logger) : base(logger)
        {
            Title = "PVP";
            AllResults = new ObservableCollection<PlayerVersusRecordGroup>();
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadAllItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                AllResults.Clear();
                var pvpResults = await PinballRankingApi.GetPlayerVersusPlayer(PlayerId);

                if (pvpResults.PlayerVersusPlayerRecords != null)
                {
                    var groupedResults = pvpResults.PlayerVersusPlayerRecords
                                                       .OrderBy(n => n.LastName)
                                                       .ThenBy(n => n.FirstName)
                                                       .GroupBy(c => char.ToUpper(c.LastName.FirstOrDefault()).ToString())
                                                       .Select(g => new PlayerVersusRecordGroup(g.Key, g));

                    foreach (var item in groupedResults)
                    {
                        AllResults.Add(item);
                    }

                }
                else
                {
                    HasNoPvpData = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading elite pvp data for player {0}", PlayerId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task LoadEliteItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                AllResults.Clear();
                var pvpResults = await PinballRankingApi.GetPlayerVersusPlayer(PlayerId);

                if (pvpResults.PlayerVersusPlayerRecords != null)
                {
                    var groupedResults = pvpResults.PlayerVersusPlayerRecords
                                                        .OrderBy(n => n.LastName)
                                                        .ThenBy(n => n.FirstName)
                                                        .GroupBy(c => char.ToUpper(c.LastName.FirstOrDefault()).ToString())
                                                        .Select(g => new PlayerVersusRecordGroup(g.Key, g));

                    foreach (var item in groupedResults)
                    {
                        AllResults.Add(item);
                    }
                }
                else
                {
                    HasNoPvpData = true;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading all pvp data for player {0}", PlayerId);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
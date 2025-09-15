using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.Universal.Players;
using Ifpa.Models;
using PinballApi;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using PinballApi.Interfaces;

namespace Ifpa.ViewModels
{
    public partial class PlayerVersusPlayerViewModel : BaseViewModel
    {
        public ObservableCollection<PlayerVersusRecordGroup> AllResults { get; set; }

        public ObservableCollection<PlayerVersusRecordGroup> EliteResults { get; set; }
        public Command LoadAllItemsCommand { get; set; }

        public Command LoadEliteItemsCommand { get; set; }

        public int PlayerId { get; set; }

        private readonly IPinballRankingApi PinballRankingApi;

        [ObservableProperty]
        private bool hasNoPvpData;

        public PlayerVersusPlayerViewModel(IPinballRankingApi pinballRankingApi, ILogger<PlayerVersusPlayerViewModel> logger) : base(logger)
        {
            Title = "PVP";
            AllResults = new ObservableCollection<PlayerVersusRecordGroup>();
            EliteResults = new ObservableCollection<PlayerVersusRecordGroup>();
            LoadAllItemsCommand = new Command(async () => await ExecuteLoadAllItemsCommand());
            LoadEliteItemsCommand = new Command(async () => await ExecuteLoadEliteItemsCommand());
            PinballRankingApi = pinballRankingApi;
        }

        async Task ExecuteLoadEliteItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                EliteResults.Clear();
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
                        EliteResults.Add(item);
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

        async Task ExecuteLoadAllItemsCommand()
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
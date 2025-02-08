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
        public ObservableCollection<Grouping<char, PlayerVersusRecord>> AllResults { get; set; }

        public ObservableCollection<Grouping<char, PlayerVersusRecord>> EliteResults { get; set; }
        public Command LoadAllItemsCommand { get; set; }

        public Command LoadEliteItemsCommand { get; set; }

        public int PlayerId { get; set; }

        private readonly IPinballRankingApi PinballRankingApi;

        [ObservableProperty]
        private bool hasNoPvpData;

        public PlayerVersusPlayerViewModel(IPinballRankingApi pinballRankingApi, ILogger<PlayerVersusPlayerViewModel> logger) : base(logger)
        {
            Title = "PVP";
            AllResults = new ObservableCollection<Grouping<char, PlayerVersusRecord>>();
            EliteResults = new ObservableCollection<Grouping<char, PlayerVersusRecord>>();
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
                    var lastNames = pvpResults.PlayerVersusPlayerRecords
                                            .OrderBy(n => n.LastName).Select(n => n.LastName).ToList();
                    var groupedResults = pvpResults.PlayerVersusPlayerRecords
                                            .OrderBy(n => n.LastName)
                                            .ThenBy(n => n.FirstName)
                                            .GroupBy(c => char.ToUpper(c.LastName.FirstOrDefault()))
                                            .Select(g => new Grouping<char, PlayerVersusRecord>(g.Key, g));

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
                    var lastNames = pvpResults.PlayerVersusPlayerRecords
                                            .OrderBy(n => n.LastName).Select(n => n.LastName).ToList();
                    var groupedResults = pvpResults.PlayerVersusPlayerRecords
                                            .OrderBy(n => n.LastName)
                                            .ThenBy(n => n.FirstName)
                                            .GroupBy(c => char.ToUpper(c.LastName.FirstOrDefault()))
                                            .Select(g => new Grouping<char, PlayerVersusRecord>(g.Key, g));

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
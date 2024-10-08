﻿using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v2.Players;
using PinballApi;
using Microsoft.Extensions.Logging;

namespace Ifpa.ViewModels
{
    public class PlayerVersusPlayerDetailViewModel : BaseViewModel
    {
        public ObservableCollection<PlayerVersusPlayerComparisonRecord> PlayerVersusPlayer { get; set; }

        public string PlayerOneInitials { get; set; }

        public string PlayerTwoInitials { get; set; }
        public Command LoadItemsCommand { get; set; }

        public int PlayerOneId { get; set; }
        public int PlayerTwoId { get; set; }
        public PlayerVersusPlayerDetailViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerVersusPlayerDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            PlayerVersusPlayer = new ObservableCollection<PlayerVersusPlayerComparisonRecord>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                PlayerVersusPlayer.Clear();
                var pvpResults = await PinballRankingApiV2.GetPlayerVersusPlayerComparison(PlayerOneId, PlayerTwoId);

                foreach (var item in pvpResults.ComparisonRecords.OrderByDescending(n => n.EventDate))
                {
                    PlayerVersusPlayer.Add(item);
                }

                Title = $"{pvpResults.PlayerOne.FirstName} {pvpResults.PlayerOne.LastName} vs {pvpResults.PlayerTwo.FirstName} {pvpResults.PlayerTwo.LastName}";

                PlayerOneInitials = pvpResults.PlayerOne.FirstName.FirstOrDefault().ToString() + pvpResults.PlayerOne.LastName.FirstOrDefault().ToString();
                PlayerTwoInitials = pvpResults.PlayerTwo.FirstName.FirstOrDefault().ToString() + pvpResults.PlayerTwo.LastName.FirstOrDefault().ToString();
                OnPropertyChanged("PlayerOneInitials");
                OnPropertyChanged("PlayerTwoInitials");
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error loading player versus player for players {0} {1}", PlayerOneId, PlayerTwoId);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
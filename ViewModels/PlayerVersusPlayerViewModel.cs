using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v2.Players;
using Ifpa.Models;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class PlayerVersusPlayerViewModel : BaseViewModel
    {
        public ObservableCollection<Grouping<char, PlayerVersusRecord>> AllResults { get; set; }

        public ObservableCollection<Grouping<char, PlayerVersusRecord>> EliteResults { get; set; }
        public Command LoadAllItemsCommand { get; set; }

        public Command LoadEliteItemsCommand { get; set; }

        public int PlayerId { get; set; }

        public bool HasNoPvpData { get; set; }

        public PlayerVersusPlayerViewModel(IConfiguration config) : base(config)
        {
            Title = "PVP";
            AllResults = new ObservableCollection<Grouping<char, PlayerVersusRecord>>();
            EliteResults = new ObservableCollection<Grouping<char, PlayerVersusRecord>>();
            LoadAllItemsCommand = new Command(async () => await ExecuteLoadAllItemsCommand());
            LoadEliteItemsCommand = new Command(async () => await ExecuteLoadEliteItemsCommand());
        }

        async Task ExecuteLoadEliteItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                EliteResults.Clear();
                var pvpResults = await PinballRankingApiV2.GetPlayerVersusElitePlayer(PlayerId);

                if (pvpResults.Records != null)
                {
                    var lastNames = pvpResults.Records
                                            .OrderBy(n => n.LastName).Select(n => n.LastName).ToList();
                    var groupedResults = pvpResults.Records
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

                    OnPropertyChanged(nameof(HasNoPvpData));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
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
                var pvpResults = await PinballRankingApiV2.GetPlayerVersusPlayer(PlayerId);

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

                    OnPropertyChanged(nameof(HasNoPvpData));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
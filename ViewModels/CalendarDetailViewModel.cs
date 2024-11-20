using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Interfaces;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.Universal.Tournaments;

namespace Ifpa.ViewModels
{
    [QueryProperty("TournamentId", "tournamentId")]
    public partial class CalendarDetailViewModel : BaseViewModel
    {
        private const string MATCHPLAY_TOURNAMENT_URL = "https://app.matchplay.events/tournaments/{0}";

        [ObservableProperty]
        private Tournament tournament;

        public int TournamentId { get; set; }

        private readonly IReminderService ReminderService;

        private readonly PinballRankingApi UniversalPinballRankingApi;


        public CalendarDetailViewModel(IReminderService reminderService, PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, ILogger<CalendarDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            UniversalPinballRankingApi = pinballRankingApi;
            ReminderService = reminderService;
        }

        [RelayCommand]
        public async Task LoadWebsite()
        {
            await Browser.OpenAsync(Tournament.Website, BrowserLaunchMode.SystemPreferred);
        }

        [RelayCommand]
        public async Task LoadMatchPlay()
        {
            await Browser.OpenAsync(string.Format(MATCHPLAY_TOURNAMENT_URL, Tournament.MatchplayId), BrowserLaunchMode.SystemPreferred);
        }

        [RelayCommand]
        public async Task ExecuteLoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Tournament = await UniversalPinballRankingApi.GetTournament(TournamentId);

                logger.LogDebug("loaded calendar item {0}", TournamentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading Calendar Item {0}", TournamentId);
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
                Title = Strings.CalendarDetailPage_SharePrompt
            });
        }

        [RelayCommand]
        public async Task OpenMap()
        {
            var placemark = new Placemark
            {
                CountryName = Tournament.CountryName,
                AdminArea = Tournament.Stateprov,
                Thoroughfare = Tournament.Address1,
                Locality = Tournament.City
            };
            var options = new MapLaunchOptions { Name = Tournament.TournamentName };

            await Map.OpenAsync(placemark, options);
        }

        [RelayCommand]
        public async Task AddToCalendar()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.CalendarRead>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.CalendarWrite>();
            }

            if (status == PermissionStatus.Granted)
            {
                string selectedCalendar = null;

                //iOS Supports multiple calendars. no idea how to do this in Android yet. 
                if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
                {
                    var calendars = await ReminderService.GetCalendarList();
                    selectedCalendar = await Shell.Current.DisplayActionSheet("This event will be added to your phone's calendar", "Cancel", null, calendars.ToArray());
                }

                if (selectedCalendar != "Cancel")
                {
                    var result = await ReminderService.CreateReminder(this, selectedCalendar);

                    if (result)
                    {
                        await Shell.Current.DisplayAlert("Success", "Tournament added to your Calendar", "OK");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Unable to add Tournament to your Calendar", "OK");
                    }
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Permission Required", "IFPA Companion requires your permission before adding items to your Calendar", "OK");
            }
        }
    }
}

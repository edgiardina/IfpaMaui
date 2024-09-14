using Ifpa.Interfaces;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.Universal.Tournaments;
using Syncfusion.Maui.Core.Carousel;
using System.Windows.Input;
using static System.Net.WebRequestMethods;


namespace Ifpa.ViewModels
{
    [QueryProperty("TournamentId", "tournamentId")]
    public class CalendarDetailViewModel : BaseViewModel
    {
        private const string MATCHPLAY_TOURNAMENT_URL = "https://app.matchplay.events/tournaments/{0}";

        public Tournament Tournament { get; set; }

        public Command LoadItemsCommand { get; set; }
        public ICommand LoadMatchPlayCommand { get; set; }
        public ICommand LoadWebsiteCommand { get; set; }

        public int TournamentId { get; set; }

        private readonly IReminderService ReminderService;

        private readonly PinballRankingApi UniveralPinballRankingApi;


        public CalendarDetailViewModel(IReminderService reminderService, PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, ILogger<CalendarDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            UniveralPinballRankingApi = pinballRankingApi;

            ReminderService = reminderService;
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            LoadMatchPlayCommand = new Command(async () => await LoadMatchPlay());
            LoadWebsiteCommand = new Command(async () => await LoadWebsite());
        }

        private async Task LoadWebsite()
        {
            await Browser.OpenAsync(Tournament.Website, BrowserLaunchMode.SystemPreferred);
        }

        private async Task LoadMatchPlay()
        {
            await Browser.OpenAsync(string.Format(MATCHPLAY_TOURNAMENT_URL, Tournament.MatchplayId), BrowserLaunchMode.SystemPreferred);
        }

        public async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Tournament = await UniveralPinballRankingApi.GetTournament(TournamentId);


                OnPropertyChanged(null);

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

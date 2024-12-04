using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.Universal.Tournaments;
using Plugin.Maui.CalendarStore;

namespace Ifpa.ViewModels
{
    [QueryProperty("TournamentId", "tournamentId")]
    [QueryProperty("AddToCalendarOnLoad", "add")]
    public partial class CalendarDetailViewModel : BaseViewModel
    {
        private const string MATCHPLAY_TOURNAMENT_URL = "https://app.matchplay.events/tournaments/{0}";

        [ObservableProperty]
        private Tournament tournament;

        public int TournamentId { get; set; }
        public bool AddToCalendarOnLoad { get; set; }

        private readonly ICalendarStore CalendarStore;
        private readonly IMap Map;

        private readonly PinballRankingApi UniversalPinballRankingApi;


        public CalendarDetailViewModel(ICalendarStore calendarStore, IMap map, PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, ILogger<CalendarDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            UniversalPinballRankingApi = pinballRankingApi;
            CalendarStore = calendarStore;
            Map = map;
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

                if (AddToCalendarOnLoad)
                {
                    await AddToCalendar();
                }

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

                var calendars = await CalendarStore.GetCalendars();
                selectedCalendar = await Shell.Current.DisplayActionSheet(Strings.CalendarDetailPage_SelectCalendarPrompt, 
                                                                          Strings.Cancel, 
                                                                          null, 
                                                                          calendars.Where(m => m.IsReadOnly == false)
                                                                                   .Select(n => n.Name)
                                                                                   .ToArray());

                if (selectedCalendar != Strings.Cancel)
                {
                    var selectedCalendarId = calendars.First(n => n.Name == selectedCalendar).Id;
                    string newEventId = null;

                    // TODO: what if the tournament is already in the calendar?

                    newEventId = await CalendarStore.CreateEvent(selectedCalendarId,
                                                    Tournament.TournamentName,
                                                    Tournament.Details,
                                                    $"{Tournament.Address1}, {Tournament.City}, {Tournament.Stateprov}, {Tournament.CountryName}",
                                                    Tournament.EventStartDate,
                                                    Tournament.EventEndDate,
                                                    true);

                    if (string.IsNullOrWhiteSpace(newEventId) == false)
                    {
                        // TODO: it would be better if Toast was an interface so we could unit test this
                        await Toast.Make(Strings.CalendarDetailPage_TournamentAdded).Show();
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert(Strings.Error, Strings.CalendarDetailPage_TournamentNotAdded, Strings.OK);
                    }
                }
            }
            else
            {
                await Shell.Current.DisplayAlert(Strings.PermissionRequired, Strings.CalendarDetailPage_AddCalendarPermissionRequest, Strings.OK);
            }
        }
    }
}

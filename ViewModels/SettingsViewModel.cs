using CommunityToolkit.Mvvm.ComponentModel;
using Ifpa.BackgroundJobs;
using Ifpa.Models;
using Ifpa.Services;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;
using Plugin.Maui.CalendarStore;
using Shiny.Jobs;

namespace Ifpa.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly AppSettings AppSettings;
        private readonly IJobManager JobManager;
        private readonly ICalendarSyncService CalendarSyncService;

        [ObservableProperty]
        private Player playerRecord;

        [ObservableProperty]
        private string playerAvatar;

        public SettingsViewModel(PinballRankingApiV2 pinballRankingApiV2, AppSettings appSettings, IJobManager jobManager, ICalendarSyncService calendarSyncService, ILogger<SettingsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            AppSettings = appSettings;
            this.JobManager = jobManager;
            CalendarSyncService = calendarSyncService;
        }

        public async Task LoadPlayer()
        {
            try
            {
                if (Settings.MyStatsPlayerId > 0)
                {
                    IsBusy = true;
                    var playerData = await PinballRankingApiV2.GetPlayer(Settings.MyStatsPlayerId);

                    PlayerRecord = playerData;
                    PlayerAvatar = PlayerRecord.ProfilePhoto != null ? PlayerRecord.ProfilePhoto?.ToString() : AppSettings.IfpaPlayerNoProfilePicUrl;
                }
                else
                {
                    PlayerRecord = null;
                    PlayerAvatar = AppSettings.IfpaPlayerNoProfilePicUrl;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading player id {0} in settings", Settings.MyStatsPlayerId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public bool NotifyOnRankChange
        {
            get => Settings.NotifyOnRankChange;
            set
            {
                Settings.NotifyOnRankChange = value;
                OnPropertyChanged(nameof(NotifyOnRankChange));
            }
        }
        public bool NotifyOnTournamentResult
        {
            get => Settings.NotifyOnTournamentResult;
            set
            {
                Settings.NotifyOnTournamentResult = value;
                OnPropertyChanged(nameof(NotifyOnTournamentResult));
            }
        }

        public bool NotifyOnNewBlogPost
        {
            get => Settings.NotifyOnNewBlogPost;
            set
            {
                Settings.NotifyOnNewBlogPost = value;
                OnPropertyChanged(nameof(NotifyOnNewBlogPost));
            }
        }

        public bool NotifyOnNewCalendarEntry
        {
            get => Settings.NotifyOnNewCalendarEntry;
            set
            {
                Settings.NotifyOnNewCalendarEntry = value;
                OnPropertyChanged(nameof(NotifyOnNewCalendarEntry));
            }
        }

        public bool SyncCalendarWithSystem
        {
            get => Settings.SyncCalendarWithSystem;
            set
            {
                Settings.SyncCalendarWithSystem = value;
                if (value)
                {
                    var hasPermission = Permissions.CheckStatusAsync<Permissions.CalendarWrite>().Result;

                    if (hasPermission == PermissionStatus.Granted)
                    {
                        JobManager.Run(nameof(CalendarSyncJob));
                    }
                    else
                    {
                        var request = Permissions.RequestAsync<Permissions.CalendarWrite>().Result;
                        if (request != PermissionStatus.Granted)
                        {
                            Settings.SyncCalendarWithSystem = false;
                        }
                        else
                        {
                            JobManager.Run(nameof(CalendarSyncJob));
                        }
                    }
                }
                else
                {
                    CalendarSyncService.DeleteIfpaDeviceCalendarAndClearLocalEvents();
                }
                OnPropertyChanged(nameof(SyncCalendarWithSystem));
            }
        }
    }
}

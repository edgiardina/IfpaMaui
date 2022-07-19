using Ifpa.Models;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        public SettingsViewModel(IConfiguration config) : base(config)
        {

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
    }
}

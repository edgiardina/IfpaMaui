using Ifpa.ViewModels;
using Ifpa.Models;
using MauiIcons.Fluent;
using MauiIcons.Core;
using CommunityToolkit.Maui.Alerts;
using static System.Net.Mime.MediaTypeNames;
using System.Threading;

namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerDetailPage : ContentPage
    {
        PlayerDetailViewModel ViewModel;

        bool LoadMyStats = false;

        public int PlayerId { get; set; }

        public PlayerDetailPage(PlayerDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (PlayerId == 0)
                LoadMyStats = true;
            else
                ViewModel.PlayerId = PlayerId;

            if (LoadMyStats)
            {
                ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == Strings.PlayerDetailPage_SetToMyStats));
                ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == Strings.PlayerDetailPage_Favorite));

                if (Settings.HasConfiguredMyStats)
                {
                    try
                    {
                        ViewModel.PlayerId = Settings.MyStatsPlayerId;
                    }
                    catch (Exception)
                    {
                        await RedirectUserToPlayerSearch();
                    }
                }
                else
                {
                    await RedirectUserToPlayerSearch();
                }

                ViewModel.PostPlayerLoadCommand = new Command(async () => await PostPlayerLoad());
            }
            else
            {
                if (Settings.HasConfiguredMyStats)
                {
                    ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == Strings.PlayerDetailPage_SetToMyStats));

                    //if player is in the existing favorites list, fill the heart icon.
                    if (await Settings.LocalDatabase.HasFavorite(ViewModel.PlayerId))
                    {
                        SetCorrectFavoriteIcon(true);
                    }
                }
                else
                {
                    ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == Strings.PlayerDetailPage_Favorite));
                }
                ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == Strings.PlayerDetailPage_ActivityFeed));
            }

            await ViewModel.ExecuteLoadItemsCommand();
        }

        /// <summary>
        /// Do tasks we need the UI to be fully re-drawn for.
        /// </summary>
        /// <returns></returns>
        private async Task PostPlayerLoad()
        {
            var numOfUnread = await Settings.LocalDatabase.GetUnreadActivityCount();

            //TODO: badge the icons in the toolbar
            //DependencyService.Get<IToolbarItemBadgeService>().SetBadge(this, ToolbarItems.SingleOrDefault(n => n.Text == "Activity Feed"), numOfUnread.ToString(), Colors.Red, Colors.White);
        }

        private async void TournamentResults_Button_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"player-results?playerId={ViewModel.PlayerId}");
        }

        private async void Pvp_Button_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"pvp?playerId={ViewModel.PlayerId}");
        }

        private async void StarButton_Clicked(object sender, EventArgs e)
        {
            try
            {
                if (!Settings.HasConfiguredMyStats)
                {
                    await ChangePlayerAndRedirect();
                }
                else
                {
                    var result = await DisplayAlert(Strings.PlayerDetailPage_Caution, Strings.PlayerDetailPage_AlreadyConfigured, Strings.OK, Strings.Cancel);
                    if (result)
                    {
                        await ChangePlayerAndRedirect();
                    }
                }
            }
            catch
            {
                await DisplayAlert(Strings.Error, Strings.PlayerDetailPage_ErrorMyStats, Strings.OK);
            }
        }

        private async Task ChangePlayerAndRedirect()
        {
            await Settings.SetMyStatsPlayer(ViewModel.PlayerId, ViewModel.PlayerRecord.PlayerStats.CurrentWpprRank);
            await ViewModel.PrepopulateTourneyResults(ViewModel.PlayerId);

            await DisplayAlert(Strings.PlayerDetailPage_Congratulations, Strings.PlayerDetailPage_ConfiguredStats, Strings.OK);

            await Shell.Current.GoToAsync("///my-stats");
        }

        private async Task RedirectUserToPlayerSearch()
        {
            await DisplayAlert(Strings.PlayerDetailPage_ConfigureYourStats, Strings.PlayerDetailPage_HaventConfiguredMyStats, Strings.OK);
            await Shell.Current.GoToAsync("///rankings/player-search");
        }

        private async void ActivityFeedButton_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("activity-feed");
        }

        private async void ShareButton_Clicked(object sender, EventArgs e)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Uri = $"https://www.ifpapinball.com/player.php?p={ViewModel.PlayerId}",
                Title = Strings.PlayerDetailPage_SharePlayer
            });
        }

        private async void FavoriteButton_Clicked(object sender, EventArgs e)
        {

            if (await Settings.LocalDatabase.HasFavorite(ViewModel.PlayerId))
            {
                await Settings.LocalDatabase.RemoveFavorite(ViewModel.PlayerId);
                SetCorrectFavoriteIcon(false);

                await Toast.Make(Strings.PlayerDetailPage_RemoveFavorite).Show();
            }
            else
            {
                await Settings.LocalDatabase.AddFavorite(ViewModel.PlayerId);
                SetCorrectFavoriteIcon(true);
                await Toast.Make(Strings.PlayerDetailPage_AddFavorite).Show();
            }
        }

        private void SetCorrectFavoriteIcon(bool isFavorite = true)
        {
            var colorDictionary = Microsoft.Maui.Controls.Application.Current.Resources.MergedDictionaries.First();
            var toolbarIconColor = (Color)colorDictionary["IconAccentColor"];
            var filledHeartIcon = (FontImageSource)new MauiIcon() { Icon = FluentIcons.Heart48, IconColor = toolbarIconColor };
            var unfilledHeartIcon = (FontImageSource)new MauiIcon() { Icon = FluentIcons.HeartBroken24, IconColor = toolbarIconColor };

            if (isFavorite)
            {
                //if player is in the existing favorites list, fill the heart icon.
                ToolbarItems.SingleOrDefault(n => n.Text == Strings.PlayerDetailPage_Favorite).IconImageSource = unfilledHeartIcon;
            }
            else
            {
                ToolbarItems.SingleOrDefault(n => n.Text == Strings.PlayerDetailPage_Favorite).IconImageSource = filledHeartIcon;
            }
        }

        private async void CS_Button_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"player-champ-series?playerId={ViewModel.PlayerId}");
        }
    }


}
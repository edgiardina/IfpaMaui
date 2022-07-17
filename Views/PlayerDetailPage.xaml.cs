using Ifpa.ViewModels;
using Ifpa.Models;

namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerDetailPage : ContentPage
    {
        PlayerDetailViewModel ViewModel;

        bool LoadMyStats = false;

        public int PlayerId { get;set; }

        public PlayerDetailPage(PlayerDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
            viewModel.IsBusy = true;
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
                ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == "Set to My Stats"));
                ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == "Favorite"));

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

                ViewModel.PostPlayerLoadCommand = new Command(async () => await PostPlayerLoad());
            }
            else
            {
                if (Settings.HasConfiguredMyStats)
                {
                    ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == "Set to My Stats"));

                    //if player is in the existing favorites list, fill the heart icon.
                    if (await Settings.LocalDatabase.HasFavorite(ViewModel.PlayerId))
                    {
                        SetCorrectFavoriteIcon(true);
                    }
                }
                else
                {
                    ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == "Favorite"));
                }
                ToolbarItems.Remove(ToolbarItems.SingleOrDefault(n => n.Text == "Activity Feed"));
            }

            ViewModel.LoadItemsCommand.Execute(null);

            //there's some sort of chart bug here so set the chart color manually again            
            //RankProgressChart.Title.TextColor = (Color)Application.Current.Resources["PrimaryTextColor"];
            //RatingProgressChart.Title.TextColor = (Color)Application.Current.Resources["PrimaryTextColor"];
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
            if (!Settings.HasConfiguredMyStats)
            {
                await ChangePlayerAndRedirect();
            }
            else
            {
                var result = await DisplayAlert("Caution", "You have already configured your Stats page, do you wish to change your Stats to this player?", "OK", "Cancel");
                if (result)
                {
                    await ChangePlayerAndRedirect();
                }
            }
        }

        private async Task ChangePlayerAndRedirect()
        {
            await Settings.SetMyStatsPlayer(ViewModel.PlayerId, ViewModel.PlayerRecord.PlayerStats.CurrentWpprRank);

            await DisplayAlert("Congratulations", "You have now configured your Stats page!", "OK");

            await Shell.Current.GoToAsync("///my-stats");
        }

        private async Task RedirectUserToPlayerSearch()
        {
            await DisplayAlert("Configure your Stats", "Looks like you haven't configured your 'My Stats' page. Use the Player Search to find your Player, and press the Star to configure your Stats", "OK");
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
                Title = "Share Player"
            });
        }

        private async void FavoriteButton_Clicked(object sender, EventArgs e)
        {

            if (await Settings.LocalDatabase.HasFavorite(ViewModel.PlayerId))
            {
                await Settings.LocalDatabase.RemoveFavorite(ViewModel.PlayerId);
                SetCorrectFavoriteIcon(false);
                await DisplayAlert("Favorite Removed", "This player has been removed from your favorites!", "OK");
            }
            else
            {
                await Settings.LocalDatabase.AddFavorite(ViewModel.PlayerId);
                SetCorrectFavoriteIcon(true);                
                await DisplayAlert("Favorite Added", "This player has been added to your favorites!", "OK");
            }
        }

        private void SetCorrectFavoriteIcon(bool isFavorite = true)
        {
            if (isFavorite)
            {
                //if player is in the existing favorites list, fill the heart icon.
                ToolbarItems.SingleOrDefault(n => n.Text == "Favorite").IconImageSource = "favorite.png";
                if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                {
                    ToolbarItems.SingleOrDefault(n => n.Text == "Favorite").IconImageSource = "favorite_white.png";
                }
                else
                {
                    ToolbarItems.SingleOrDefault(n => n.Text == "Favorite").IconImageSource = "favorite.png";
                }
            }
            else
            {                
                if (DeviceInfo.Current.Platform == DevicePlatform.Android)
                {
                    ToolbarItems.SingleOrDefault(n => n.Text == "Favorite").IconImageSource = "favorite_outline.png";
                }
                else
                {
                    ToolbarItems.SingleOrDefault(n => n.Text == "Favorite").IconImageSource = "favorite-outline.png";
                }
            }
        }

        private async void CS_Button_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"player-champ-series?playerId={ViewModel.PlayerId}");
        }
    }


}
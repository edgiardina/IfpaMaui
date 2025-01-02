using CommunityToolkit.Maui.Alerts;
using Ifpa.Interfaces;
using Ifpa.Models;
using Ifpa.ViewModels;
using MauiIcons.Core;
using MauiIcons.Fluent;
using Microsoft.Maui.Layouts;

namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerDetailPage : ContentPage
    {
        public int PlayerId { get; set; }

        private bool LoadMyStats = false;

        private readonly PlayerDetailViewModel ViewModel;

        private readonly IToolbarBadgeService toolbarBadgeService;

        public PlayerDetailPage(PlayerDetailViewModel viewModel, IToolbarBadgeService toolbarBadgeService)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
            this.toolbarBadgeService = toolbarBadgeService;
        }

        protected async override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

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

            await ViewModel.LoadItems();

            //if loading My Stats player, refresh the activity feed counter.
            if (LoadMyStats)
            {
                toolbarBadgeService.SetBadge(this, ActivityFeedButton, ViewModel.BadgeCount, Colors.Red, Colors.White);
            }
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
            await Shell.Current.GoToAsync("/player-details-no-player-selected");
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
                Title = Strings.PlayerDetailPage_SharePlayer,

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

        private bool isAvatarEnlarged = false;

        private async void OnAvatarTapped(object sender, EventArgs e)
        {
            if (isAvatarEnlarged)
            {
                // Shrink enlarged avatar back to original size and position
                EnlargedPlayerAvatar.ScaleTo(1, 250, Easing.CubicOut);
                EnlargedPlayerAvatar.TranslateTo(0, 0, 250, Easing.CubicOut);

                AvatarOverlay.IsVisible = false;
                PlayerAvatar.IsVisible = true;

                isAvatarEnlarged = false;
            }
            else
            {
                // Set EnlargedPlayerAvatar to match PlayerAvatar's size and position initially
                AvatarOverlay.IsVisible = true;

                // Set EnlargedPlayerAvatar to the initial position of PlayerAvatar
                AbsoluteLayout.SetLayoutBounds(EnlargedPlayerAvatar, new Rect(PlayerAvatar.X, PlayerAvatar.Y, PlayerAvatar.Width, PlayerAvatar.Height));
                AbsoluteLayout.SetLayoutFlags(EnlargedPlayerAvatar, AbsoluteLayoutFlags.None);

                PlayerAvatar.IsVisible = false; // Hide original avatar

                // Calculate the target size and position for the enlarged avatar
                double screenWidth = this.Window.Width;
                double screenHeight = this.Window.Height;

                double targetWidth = screenWidth * 0.8;
                double targetHeight = screenHeight * 0.8;

                // Calculate the center position for the enlarged avatar
                double targetX = (screenWidth - targetWidth) / 2;
                double targetY = (screenHeight - targetHeight) / 2;

                // Set the final bounds explicitly to ensure it's centered
                AbsoluteLayout.SetLayoutBounds(EnlargedPlayerAvatar, new Rect(0.5, 0.5, targetWidth, targetHeight));
                AbsoluteLayout.SetLayoutFlags(EnlargedPlayerAvatar, AbsoluteLayoutFlags.PositionProportional);

                // Animate to the center with the new size
                //EnlargedPlayerAvatar.TranslateTo(targetX, targetY - PlayerAvatar.Y, 250, Easing.CubicOut);
                EnlargedPlayerAvatar.ScaleTo(targetWidth / PlayerAvatar.Width, 250, Easing.CubicOut);

                isAvatarEnlarged = true;
            }
        }

    }


}
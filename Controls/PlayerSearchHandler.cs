﻿using Ifpa.ViewModels;
using PinballApi.Models.WPPR.Universal.Players;

namespace Ifpa.Controls
{
    public class PlayerSearchHandler : SearchHandler
    {
        readonly PlayerSearchViewModel viewModel;

        public PlayerSearchHandler()
        {
            //TODO: in a perfect world we'd be able to DI this
            viewModel = Application.Current.Handler.MauiContext.Services.GetService<PlayerSearchViewModel>();
        }

        protected override async void OnQueryChanged(string oldValue, string newValue)
        {
            base.OnQueryChanged(oldValue, newValue);

            if (string.IsNullOrWhiteSpace(newValue) || newValue.Length <= 2)
            {
                ItemsSource = null;
            }
            else
            {
                viewModel.Text = newValue;
                await viewModel.Search();

                ItemsSource = viewModel.Players;
            }
        }

        protected override async void OnItemSelected(object item)
        {
            base.OnItemSelected(item);

            this.Unfocus();

            //TODO: Remove when https://github.com/dotnet/maui/issues/16298 is fixed
            Platforms.KeyboardHelper.HideKeyboard();

            // The following route works because route names are unique in this app.
            await Shell.Current.GoToAsync($"player-details?playerId={((Player)item).PlayerId}");
        }

    }
}

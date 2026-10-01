using Ifpa.Views;

namespace Ifpa.Models
{
    public static class ShellNavigationExtensions
    {
        /// <summary>
        /// Navigates to a player's details. If that player's details page is already in the current
        /// navigation stack, pops back to it instead of pushing a duplicate.
        /// </summary>
        public static async Task GoToPlayerDetailsAsync(this Shell shell, long playerId)
        {
            var section = shell.CurrentItem?.CurrentItem;
            var stack = section?.Navigation.NavigationStack;

            if (stack != null && playerId <= int.MaxValue)
            {
                // Shell keeps a null placeholder for the section's root page; resolve it so a root
                // player page (e.g. My Stats) can be matched too.
                var rootPage = (section.CurrentItem as IShellContentController)?.Page;

                var stackPlayerIds = stack
                    .Select((page, index) => index == 0 && page == null ? rootPage : page)
                    .Select(page => page is PlayerDetailPage playerPage ? playerPage.DisplayedPlayerId : (int?)null)
                    .ToList();

                var pagesToPop = PlayerNavigationStack.PagesToPopTo(stackPlayerIds, (int)playerId);

                if (pagesToPop == 0)
                    return;

                if (pagesToPop.HasValue)
                {
                    await shell.GoToAsync(string.Join("/", Enumerable.Repeat("..", pagesToPop.Value)));
                    return;
                }
            }

            await shell.GoToAsync($"player-details?playerId={playerId}");
        }
    }
}

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

            if (section != null)
            {
                var stack = section.Navigation.NavigationStack;

                for (int i = stack.Count - 1; i >= 0; i--)
                {
                    // Shell keeps a null placeholder for the section's root page; resolve it so a root
                    // player page (e.g. My Stats) can be matched too.
                    var page = stack[i] ?? (i == 0 ? (section.CurrentItem as IShellContentController)?.Page : null);

                    if (page is PlayerDetailPage playerPage && playerPage.DisplayedPlayerId == playerId)
                    {
                        var pagesToPop = stack.Count - 1 - i;
                        if (pagesToPop > 0)
                            await shell.GoToAsync(string.Join("/", Enumerable.Repeat("..", pagesToPop)));

                        return;
                    }
                }
            }

            await shell.GoToAsync($"player-details?playerId={playerId}");
        }
    }
}

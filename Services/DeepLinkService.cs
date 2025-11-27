using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using System.Web;

namespace Ifpa.Services
{
    public class DeepLinkService : IDeepLinkService
    {
        private readonly IDispatcher _dispatcher;
        private readonly IPinballRankingApi _rankingApi;
        private readonly ILogger _logger;

        public DeepLinkService(IDispatcher dispatcher, IPinballRankingApi pinballRankingApi, ILogger<DeepLinkService> logger)
        {
            _dispatcher = dispatcher;
            _rankingApi = pinballRankingApi;
            _logger = logger;
        }

        public async Task HandleDeepLink(Uri uri)
        {
            if (uri == null)
                return;

            var route = await BuildRouteFromUri(uri);
            if (!string.IsNullOrEmpty(route))
            {
                await NavigateToRoute(route);
            }
        }

        public async Task HandleAppAction(string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
                return;

            // Handle special case for search action
            if (actionId == "rankings/player-search")
            {
                await _dispatcher.DispatchAsync(async () =>
                {
                    try
                    {
                        await Shell.Current.GoToAsync("///rankings?showSearch=true");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error navigating to player search");
                    }
                });
                return;
            }

            var route = $"//{actionId}";
            await NavigateToRoute(route);
        }

        private async Task<string> BuildRouteFromUri(Uri uri)
        {
            var uriString = uri.ToString();

            if (uriString.Contains("player.php"))
            {
                var id = HttpUtility.ParseQueryString(uri.Query)["p"];
                if (!string.IsNullOrEmpty(id))
                {
                    return $"//rankings/player-details?playerId={id}";
                }
            }
            else if (uriString.Contains("tournaments/view.php"))
            {
                var id = HttpUtility.ParseQueryString(uri.Query)["t"];
                if (!string.IsNullOrEmpty(id))
                {
                    try
                    {
                        var tournamentId = int.Parse(id);
                        var tournamentResults = await _rankingApi.GetTournamentResults(tournamentId);
                        
                        if (tournamentResults.Results != null && tournamentResults.Results.Any())
                        {
                            _logger.LogInformation("Tournament {id} has results, going to tournament results page", id);
                            return $"//rankings/tournament-results?tournamentId={id}";
                        }
                        else
                        {
                            _logger.LogInformation("Tournament {id} has no results, going to calendar details page", id);
                            return $"//calendar/calendar-detail?tournamentId={id}";
                        }
                    }
                    catch
                    {
                        return $"//calendar/calendar-detail?tournamentId={id}";
                    }
                }
            }

            return null;
        }

        private async Task NavigateToRoute(string route)
        {
            await _dispatcher.DispatchAsync(async () =>
            {
                var shell = (AppShell)Shell.Current;
                shell.ConfirmSelectedTabIsCorrect(route);
                await Task.Delay(100); // Reduced delay for tab selection
                await Shell.Current.GoToAsync(route);

                // Double check tab selection after navigation
                await Task.Delay(100);
                shell.ConfirmSelectedTabIsCorrect(route);
            });
        }
    }
}
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Extensions;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Players;
using Shiny.AppFunctions;

namespace Ifpa.AppFunctions;

[AppFunction("get_my_rank",
    Title = "My IFPA Rank",
    Description = "Gets the current IFPA world rank, WPPR points, and championship series standings of the player selected in My Stats")]
[AppShortcut("What's my ${applicationName} rank", ShortTitle = "My Rank", SystemImage = "trophy")]
[AppShortcut("${applicationName} rank")]
[AppShortcut("My ${applicationName} rank")]
[AppShortcut("Check my ${applicationName} rank")]
[AppShortcut("Get my rank in ${applicationName}")]
[AppShortcut("Show my rank in ${applicationName}")]
[AppShortcut("What's my ${applicationName} standing")]
[AppShortcut("${applicationName} standings")]
public record GetMyRank() : IAppFunction<MyRankResult>;

public record MyRankResult(string PlayerName, long Rank, double WpprPoints, IReadOnlyList<SeriesStanding> Standings);

public record SeriesStanding(string Series, string Region, long Rank);

public class GetMyRankHandler(IPinballRankingApi pinballRankingApi, PendingAssistantRoute pendingRoute, ILogger<GetMyRankHandler> logger)
    : IAppFunctionHandler<GetMyRank, MyRankResult>
{
    // Siri speaks the answer. More standings than this make the answer too long.
    const int MaxSpokenStandings = 3;

    // An assistant can start the app with no UI. Do not touch Shell or pages here.
    public async Task<MyRankResult> Handle(GetMyRank request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        if (!Settings.HasConfiguredMyStats)
            throw new AppFunctionException(AppFunctionErrorCode.NotFound, Strings.AppFunctions_NoPlayerSelected);

        try
        {
            var player = await pinballRankingApi.GetPlayer(Settings.MyStatsPlayerId);
            var stats = player.PlayerStats.Open;
            var playerName = $"{player.FirstName} {player.LastName}";

            if (stats == null)
                throw new AppFunctionException(AppFunctionErrorCode.NotFound, string.Format(Strings.AppFunctions_MyRank_NotRanked, playerName));

            var result = new MyRankResult(
                playerName,
                stats.CurrentRank,
                Convert.ToDouble(stats.CurrentPoints),
                GetStandings(player));

            // The app is in the background. If the user opens it from the answer, show My Stats.
            if (!context.IsForeground)
                pendingRoute.Set("my-stats");

            var dialog = string.Format(Strings.AppFunctions_MyRank_Dialog, result.PlayerName, stats.CurrentRank.OrdinalSuffix(), result.WpprPoints);

            if (result.Standings.Count > 0)
                dialog += " " + DescribeStandings(result.Standings);

            context.Say(dialog);

            return result;
        }
        catch (AppFunctionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading rank for player {PlayerId}", Settings.MyStatsPlayerId);
            throw new AppFunctionException(AppFunctionErrorCode.AppError, Strings.AppFunctions_MyRank_Error, ex);
        }
    }

    // The player record has the championship series standings, so this does not need a second API call.
    static List<SeriesStanding> GetStandings(Player player)
    {
        return (player.Series ?? [])
            .Where(n => n.Year == DateTime.Now.Year)
            .OrderBy(n => n.Rank)
            .Select(n => new SeriesStanding(n.SeriesCode.ToString(), n.RegionName ?? n.RegionCode, n.Rank))
            .ToList();
    }

    static string DescribeStandings(IReadOnlyList<SeriesStanding> standings)
    {
        var spoken = standings
            .Take(MaxSpokenStandings)
            .Select(n => string.Format(Strings.AppFunctions_MyRank_StandingItem, n.Rank.OrdinalSuffix(), n.Region, n.Series))
            .ToList();

        if (standings.Count > MaxSpokenStandings)
            spoken.Add(string.Format(Strings.AppFunctions_MyRank_StandingsMore, standings.Count - MaxSpokenStandings));

        return string.Format(Strings.AppFunctions_MyRank_Standings, DateTime.Now.Year, string.Join(", ", spoken));
    }
}

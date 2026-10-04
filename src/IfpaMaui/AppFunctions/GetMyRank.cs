using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Extensions;
using PinballApi.Interfaces;
using Shiny.AppFunctions;

namespace Ifpa.AppFunctions;

[AppFunction("get_my_rank",
    Title = "My IFPA Rank",
    Description = "Gets the current IFPA world rank and WPPR points of the player selected in My Stats")]
[AppShortcut("What's my ${applicationName} rank", ShortTitle = "My Rank", SystemImage = "trophy")]
[AppShortcut("${applicationName} rank")]
public record GetMyRank() : IAppFunction<MyRankResult>;

public record MyRankResult(string PlayerName, long Rank, double WpprPoints);

public class GetMyRankHandler(IPinballRankingApi pinballRankingApi, ILogger<GetMyRankHandler> logger)
    : IAppFunctionHandler<GetMyRank, MyRankResult>
{
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
                Convert.ToDouble(stats.CurrentPoints));

            context.Say(string.Format(Strings.AppFunctions_MyRank_Dialog, result.PlayerName, stats.CurrentRank.OrdinalSuffix(), result.WpprPoints));

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
}

using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using Ifpa.Exceptions;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Director;
using PinballApi.Models.WPPR.Universal.Directors;
using PinballApi.Models.WPPR.Universal.Players;
using PinballApi.Models.WPPR.Universal.Players.Search;
using PinballApi.Models.WPPR.Universal.Rankings;
using PinballApi.Models.WPPR.Universal.Rankings.Custom;
using PinballApi.Models.WPPR.Universal.Series;
using PinballApi.Models.WPPR.Universal.Stats;
using PinballApi.Models.WPPR.Universal.Tournaments;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using Polly;
using Polly.Caching;

namespace Ifpa.Caching
{
    public class CachingPinballRankingApi : IPinballRankingApi
    {
        private readonly IPinballRankingApi onlineApi;
        private readonly ILogger<CachingPinballRankingApi> logger;

        public CachingPinballRankingApi(IPinballRankingApi onlineApi, ILogger<CachingPinballRankingApi> logger)
        {
            this.onlineApi = onlineApi ?? throw new ArgumentNullException(nameof(onlineApi));
            this.logger = logger;
        }

        private async Task<T> ExecuteWithCache<T>(string cacheKey, Func<Task<T>> fetch)
        {
            var cache = new SQLiteCacheProvider<T>(Settings.CacheDatabasePath);

            // Retry on non network‐unavailable errors
            var retry = Policy<T>
                .Handle<Exception>(ex => ex is not NetworkUnavailableException)
                .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(250 * (1 << (i - 1))));

            // Fallback to cache if the network is unavailable or if the fetch fails
            var fallback = Policy<T>
                .Handle<Exception>()
                .FallbackAsync(
                    async (outcome, ctx, ct) =>
                    {
                        var (hit, val) = await cache.TryGetAsync(
                                                ctx.OperationKey!,
                                                ct,
                                                continueOnCapturedContext: false);

                        if (!hit)
                        {
                            logger.LogWarning(outcome.Exception,
                                "Cache miss for {Key}", ctx.OperationKey);

                            MainThread.BeginInvokeOnMainThread(() =>
                                Toast.Make(Strings.Toast_Offline_NoCache,
                                           ToastDuration.Long).Show());

                            throw outcome.Exception!;   // no cache -> real error
                        }

                        MainThread.BeginInvokeOnMainThread(() =>
                            Toast.Make(Strings.Toast_Offline_Cache,
                                       ToastDuration.Long).Show());
                        return (T)val!;
                    },
                    onFallbackAsync: async (outcome, ctx) =>
                    {
                        logger.LogWarning(outcome.Exception,
                            "Falling back to cache for {Key}", ctx.OperationKey);
                        await Task.CompletedTask;
                    });

            // wrap fallback around retry
            var pipeline = Policy.WrapAsync(fallback, retry);

            // execute: if offline, retry will see NetworkUnavailableException
            //    and skip directly to fallback; if online, fetch runs, then we cache it
            var q = await pipeline.ExecuteAndCaptureAsync(async (ctx) =>
            {
                // network‐unavailable check
                var access = Connectivity.Current.NetworkAccess;
                if (access is NetworkAccess.None or NetworkAccess.Local)
                    throw new NetworkUnavailableException();

                // real network call
                var result = await fetch().ConfigureAwait(false);

                // write‐through cache
                await cache.PutAsync(ctx.OperationKey!,
                                     result,
                                     new Ttl(90.Days()),
                                     CancellationToken.None,
                                     continueOnCapturedContext: false);

                return result;
            },
            new Context(cacheKey));
            return q.Result;
        }

        public Task<List<CountryDirector>> GetCountryDirectors() =>
            ExecuteWithCache(
                $"CountryDirectors",
                () => onlineApi.GetCountryDirectors());

        public Task<List<CustomRankingView>> GetCustomRankings() =>
            ExecuteWithCache(
                $"CustomRankings",
                () => onlineApi.GetCustomRankings());

        public Task<CustomRankingViewResult> GetCustomRankingViewResult(int viewId, int count = 50) =>
            ExecuteWithCache(
                $"CustomRankingViewResult:{viewId}:{count}",
                () => onlineApi.GetCustomRankingViewResult(viewId, count));

        public Task<Director> GetDirector(long directorId) =>
            ExecuteWithCache(
                $"Director:{directorId}",
                () => onlineApi.GetDirector(directorId));

        public Task<List<Director>> GetDirectorsBySearch(string name, int count = 50) =>
            ExecuteWithCache(
                $"DirectorsBySearch:{name}:{count}",
                () => onlineApi.GetDirectorsBySearch(name, count));

        public Task<PinballApi.Models.WPPR.Universal.Tournaments.Tournament> GetDirectorTournaments(long directorId, TimePeriod timePeriod) =>
            ExecuteWithCache(
                $"DirectorTournaments:{directorId}:{timePeriod}",
                () => onlineApi.GetDirectorTournaments(directorId, timePeriod));

        public Task<List<EventsByYearStatistics>> GetEventsByYearStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main) =>
            ExecuteWithCache(
                $"EventsByYearStats:{playerSystem}",
                () => onlineApi.GetEventsByYearStatistics(playerSystem));

        public Task<List<LargestTournamentStatistics>> GetLargestTournamentStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main) =>
            ExecuteWithCache(
                $"LargestTournamentStats:{playerSystem}",
                () => onlineApi.GetLargestTournamentStatistics(playerSystem));

        public Task<List<League>> GetLeagues(LeagueTimePeriod timePeriod) =>
            ExecuteWithCache(
                $"Leagues:{timePeriod}",
                () => onlineApi.GetLeagues(timePeriod));

        public Task<List<LucrativeTournamentStatistics>> GetLucrativeTournamentStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main) =>
            ExecuteWithCache(
                $"LucrativeTournamentStats:{playerSystem}",
                () => onlineApi.GetLucrativeTournamentStatistics(playerSystem));

        public Task<OverallStatistics> GetOverallStatistics() =>
            ExecuteWithCache(
                $"OverallStatistics",
                () => onlineApi.GetOverallStatistics());

        public Task<Player> GetPlayer(int playerId) =>
            ExecuteWithCache(
                $"Player:{playerId}",
                () => onlineApi.GetPlayer(playerId));

        public Task<PlayerHistory> GetPlayerHistory(int playerId, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, bool activeResultsOnly = false) =>
            ExecuteWithCache(
                $"PlayerHistory:{playerId}:{playerSystem}:{activeResultsOnly}",
                () => onlineApi.GetPlayerHistory(playerId, playerSystem, activeResultsOnly));

        public Task<PlayerResults> GetPlayerResults(int playerId, PlayerRankingSystem rankingSystem = PlayerRankingSystem.Main, ResultType resultType = ResultType.Active) =>
            ExecuteWithCache(
                $"PlayerResults:{playerId}:{rankingSystem}:{resultType}",
                () => onlineApi.GetPlayerResults(playerId, rankingSystem, resultType));

        public Task<List<Player>> GetPlayers(List<int> playerIds) =>
            ExecuteWithCache(
                $"Players:{string.Join(",", playerIds)}",
                () => onlineApi.GetPlayers(playerIds));

        public Task<List<PlayersByCountryStatistics>> GetPlayersByCountryStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main) =>
            ExecuteWithCache(
                $"PlayersByCountryStats:{playerSystem}",
                () => onlineApi.GetPlayersByCountryStatistics(playerSystem));

        public Task<List<PlayersByStateStatistics>> GetPlayersByStateStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main) =>
            ExecuteWithCache(
                $"PlayersByStateStats:{playerSystem}",
                () => onlineApi.GetPlayersByStateStatistics(playerSystem));

        public Task<List<PlayersByYearStatistics>> GetPlayersByYearStatistics() =>
            ExecuteWithCache(
                $"PlayersByYearStats",
                () => onlineApi.GetPlayersByYearStatistics());

        public Task<List<PlayersEventsAttendedByGivenPeriodStatistics>> GetPlayersEventsAttendedByGivenPeriod(DateOnly startDate, DateOnly endDate, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, int limit = 25) =>
            ExecuteWithCache(
                $"PlayersEvents:{startDate:o}:{endDate:o}:{playerSystem}:{limit}",
                () => onlineApi.GetPlayersEventsAttendedByGivenPeriod(startDate, endDate, playerSystem, limit));

        public Task<List<PlayersPointsByGivenPeriodStatistics>> GetPlayersPointsByGivenPeriod(DateOnly startDate, DateOnly endDate, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, int limit = 25) =>
            ExecuteWithCache(
                $"PlayersPoints:{startDate:o}:{endDate:o}:{playerSystem}:{limit}",
                () => onlineApi.GetPlayersPointsByGivenPeriod(startDate, endDate, playerSystem, limit));

        public Task<PlayerVersusPlayer> GetPlayerVersusPlayer(int playerId, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main) =>
            ExecuteWithCache(
                $"PlayerVersusPlayer:{playerId}:{playerSystem}",
                () => onlineApi.GetPlayerVersusPlayer(playerId, playerSystem));

        public Task<PlayerVersusPlayerComparison> GetPlayerVersusPlayerComparison(int playerId, int comparisonPlayerId) =>
            ExecuteWithCache(
                $"PlayerVersusComparison:{playerId}:{comparisonPlayerId}",
                () => onlineApi.GetPlayerVersusPlayerComparison(playerId, comparisonPlayerId));

        public Task<RankingCountries> GetRankingCountries() =>
            ExecuteWithCache(
                $"RankingCountries",
                () => onlineApi.GetRankingCountries());

        public Task<List<RegionRepresentative>> GetRegionReps(string seriesCode) =>
            ExecuteWithCache(
                $"RegionReps:{seriesCode}",
                () => onlineApi.GetRegionReps(seriesCode));

        public Task<List<PinballApi.Models.WPPR.Universal.Series.Region>> GetRegions(string seriesCode, int year) =>
            ExecuteWithCache(
                $"Regions:{seriesCode}:{year}",
                () => onlineApi.GetRegions(seriesCode, year));

        public Task<List<TournamentResult>> GetRelatedResults(int tournamentId) =>
            ExecuteWithCache(
                $"RelatedResults:{tournamentId}",
                () => onlineApi.GetRelatedResults(tournamentId));

        public Task<List<Series>> GetSeries() =>
            ExecuteWithCache(
                $"Series",
                () => onlineApi.GetSeries());

        public Task<SeriesOverallResults> GetSeriesOverallStanding(string seriesCode, int? year = null) =>
            ExecuteWithCache(
                $"SeriesOverall:{seriesCode}:{year}",
                () => onlineApi.GetSeriesOverallStanding(seriesCode, year));

        public Task<SeriesPlayerCard> GetSeriesPlayerCard(int playerId, string seriesCode, string region, int? year = null) =>
            ExecuteWithCache(
                $"SeriesPlayerCard:{playerId}:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesPlayerCard(playerId, seriesCode, region, year));

        public Task<RegionStandings> GetSeriesStandingsForRegion(string seriesCode, string region, int? year = null) =>
            ExecuteWithCache(
                $"SeriesStandings:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesStandingsForRegion(seriesCode, region, year));

        public Task<SeriesStats> GetSeriesStats(string seriesCode, string region, int? year = null) =>
            ExecuteWithCache(
                $"SeriesStats:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesStats(seriesCode, region, year));

        public Task<SeriesTournaments> GetSeriesTournamentsForRegion(string seriesCode, string region, int? year = null) =>
            ExecuteWithCache(
                $"SeriesTournaments:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesTournamentsForRegion(seriesCode, region, year));

        public Task<SeriesWinners> GetSeriesWinners(string seriesCode, string region = null) =>
            ExecuteWithCache(
                $"SeriesWinners:{seriesCode}:{region}",
                () => onlineApi.GetSeriesWinners(seriesCode, region));

        public Task<PinballApi.Models.WPPR.Universal.Tournaments.Tournament> GetTournament(int tournamentId) =>
            ExecuteWithCache(
                $"Tournament:{tournamentId}",
                () => onlineApi.GetTournament(tournamentId));

        public Task<TournamentFormats> GetTournamentFormats() =>
            ExecuteWithCache(
                $"TournamentFormats",
                () => onlineApi.GetTournamentFormats());

        public Task<TournamentResults> GetTournamentResults(int tournamentId) =>
            ExecuteWithCache(
                $"TournamentResults:{tournamentId}",
                () => onlineApi.GetTournamentResults(tournamentId));

        public Task<List<TournamentsByStateStatistics>> GetTournamentsByStateStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main) =>
            ExecuteWithCache(
                $"TournamentsByStateStats:{playerSystem}",
                () => onlineApi.GetTournamentsByStateStatistics(playerSystem));

        public Task<PlayerSearch> PlayerSearch(string name = null, string country = null, string stateProv = null, string tournamentName = null) =>
            ExecuteWithCache(
                $"PlayerSearch:{name}:{country}:{stateProv}:{tournamentName}",
                () => onlineApi.PlayerSearch(name, country, stateProv, tournamentName));

        public Task<ProRankingSearch> ProRankingSearch(TournamentType rankingSystem) =>
            ExecuteWithCache(
                $"ProRankingSearch:{rankingSystem}",
                () => onlineApi.ProRankingSearch(rankingSystem));

        public Task<RankingSearch> RankingSearch(RankingType rankingType, RankingSystem rankingSystem = RankingSystem.Open, int count = 100, int startPosition = 1, string countryCode = null) =>
            ExecuteWithCache(
                $"RankingSearch:{rankingType}:{rankingSystem}:{count}:{startPosition}:{countryCode}",
                () => onlineApi.RankingSearch(rankingType, rankingSystem, count, startPosition, countryCode));

        public Task<TournamentSearch> TournamentSearch(
            double? latitude = null,
            double? longitude = null,
            int? radius = null,
            DistanceType? distanceType = null,
            string name = null,
            string country = null,
            string stateprov = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            TournamentType? tournamentType = null,
            int? startPosition = null,
            int? totalReturn = null,
            TournamentSearchSortMode? tournamentSearchSortMode = null,
            TournamentSearchSortOrder? tournamentSearchSortOrder = null,
            string directorName = null,
            bool? preRegistration = null,
            bool? onlyWithResults = null,
            double? minimumPoints = null,
            double? maximumPoints = null,
            bool? pointFilter = null,
            TournamentEventType? tournamentEventType = null)
        {
            var key = $"TournamentSearch:" +
                      $"{latitude}:{longitude}:{radius}:{distanceType}:" +
                      $"{name}:{country}:{stateprov}:" +
                      $"{startDate:o}:{endDate:o}:{tournamentType}:" +
                      $"{startPosition}:{totalReturn}:{tournamentSearchSortMode}:" +
                      $"{tournamentSearchSortOrder}:{directorName}:" +
                      $"{preRegistration}:{onlyWithResults}:" +
                      $"{minimumPoints}:{maximumPoints}:{pointFilter}:" +
                      $"{tournamentEventType}";
            return ExecuteWithCache(key, () => onlineApi.TournamentSearch(
                latitude, longitude, radius, distanceType, name,
                country, stateprov, startDate, endDate, tournamentType,
                startPosition, totalReturn, tournamentSearchSortMode,
                tournamentSearchSortOrder, directorName,
                preRegistration, onlyWithResults, minimumPoints,
                maximumPoints, pointFilter, tournamentEventType));
        }
    }
}

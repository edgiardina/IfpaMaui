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
using PinballApi.Models.WPPR.Universal.Other;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using Polly;
using Polly.Caching;

namespace Ifpa.Caching
{
    public class CachingPinballRankingApi : IPinballRankingApi
    {
        private readonly IPinballRankingApi onlineApi;
        private readonly ILogger<CachingPinballRankingApi> logger;
        private readonly ILoggerFactory loggerFactory;

        public CachingPinballRankingApi(IPinballRankingApi onlineApi, ILogger<CachingPinballRankingApi> logger, ILoggerFactory loggerFactory)
        {
            this.onlineApi = onlineApi ?? throw new ArgumentNullException(nameof(onlineApi));
            this.logger = logger;
            this.loggerFactory = loggerFactory;
        }

        private async Task<T> ExecuteWithCache<T>(string cacheKey, Func<Task<T>> fetch)
        {
            // Build the cache provider defensively. A corrupt cache self-heals inside the provider,
            // but if it still can't be initialized (e.g. disk full) we degrade to a live-only fetch
            // rather than letting the failure escape the pipeline and break every cached call.
            SQLiteCacheProvider<T> cache = null;
            try
            {
                cache = new SQLiteCacheProvider<T>(Settings.CacheDatabasePath, loggerFactory);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize cache database; serving {Key} without cache", cacheKey);
            }

            // Retry on non network‐unavailable errors
            var retry = Policy<T>
                .Handle<Exception>(ex => ex is not NetworkUnavailableException and not OperationCanceledException)
                .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(250 * (1 << (i - 1))));

            // Fallback to cache if the network is unavailable or if the fetch fails
            var fallback = Policy<T>
                .Handle<Exception>(ex => ex is not OperationCanceledException)
                .FallbackAsync(
                    async (outcome, ctx, ct) =>
                    {
                        if (cache != null)
                        {
                            var (hit, val) = await cache.TryGetAsync(
                                                    ctx.OperationKey!,
                                                    ct,
                                                    continueOnCapturedContext: false);

                            if (hit)
                            {
                                MainThread.BeginInvokeOnMainThread(() =>
                                {
                                    try { Toast.Make(Strings.Toast_Offline_Cache, ToastDuration.Long).Show(); }
                                    catch (Exception toastEx) { logger.LogWarning(toastEx, "Could not show offline toast"); }
                                });
                                return (T)val!;
                            }
                        }

                        // No cache (unavailable or miss) -> surface the real error.
                        logger.LogWarning(outcome.Exception,
                            "Cache miss for {Key}", ctx.OperationKey);

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            try { Toast.Make(Strings.Toast_Offline_NoCache, ToastDuration.Long).Show(); }
                            catch (Exception toastEx) { logger.LogWarning(toastEx, "Could not show offline toast"); }
                        });

                        throw outcome.Exception!;
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
            //    and skip directly to fallback; if online, fetch runs, then we cache it.
            // ExecuteAsync (not ExecuteAndCaptureAsync) so a fallback that rethrows the real
            // error propagates to the caller instead of being swallowed into a null result.
            return await pipeline.ExecuteAsync(async (ctx) =>
            {
                // network‐unavailable check
                var access = Connectivity.Current.NetworkAccess;
                if (access is NetworkAccess.None or NetworkAccess.Local)
                    throw new NetworkUnavailableException();

                // real network call
                var result = await fetch().ConfigureAwait(false);

                // write‐through cache (skipped if the cache could not be initialized).
                // A cache-write failure (e.g. disk full) must never discard a successful live
                // fetch, so swallow it here rather than let it escape and re-trigger the pipeline.
                if (cache != null)
                {
                    try
                    {
                        await cache.PutAsync(ctx.OperationKey!,
                                             result,
                                             new Ttl(90.Days()),
                                             CancellationToken.None,
                                             continueOnCapturedContext: false);
                    }
                    catch (Exception cacheEx)
                    {
                        logger.LogWarning(cacheEx, "Failed to write cache for {Key}; returning live result", ctx.OperationKey);
                    }
                }

                return result;
            },
            new Context(cacheKey));
        }

        public Task<List<CountryDetail>> GetCountriesList(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"CountriesList",
                () => onlineApi.GetCountriesList(cancellationToken));

        public Task<List<StateProvCountry>> GetStateProvList(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"StateProvList",
                () => onlineApi.GetStateProvList(cancellationToken));

        public Task<List<CountryDirector>> GetCountryDirectors(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"CountryDirectors",
                () => onlineApi.GetCountryDirectors(cancellationToken));

        public Task<List<CustomRankingView>> GetCustomRankings(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"CustomRankings",
                () => onlineApi.GetCustomRankings(cancellationToken));

        public Task<CustomRankingViewResult> GetCustomRankingViewResult(int viewId, int count = 50, int startPosition = 1, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"CustomRankingViewResult:{viewId}:{count}:{startPosition}",
                () => onlineApi.GetCustomRankingViewResult(viewId, count, startPosition, cancellationToken));

        public Task<Director> GetDirector(long directorId, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"Director:{directorId}",
                () => onlineApi.GetDirector(directorId, cancellationToken));

        public Task<List<Director>> GetDirectorsBySearch(string name, int count = 50, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"DirectorsBySearch:{name}:{count}",
                () => onlineApi.GetDirectorsBySearch(name, count, cancellationToken));

        public Task<List<PinballApi.Models.WPPR.Universal.Tournaments.Tournament>> GetDirectorTournaments(long directorId, TimePeriod timePeriod, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"DirectorTournaments:{directorId}:{timePeriod}",
                () => onlineApi.GetDirectorTournaments(directorId, timePeriod, cancellationToken));

        public Task<List<PinballApi.Models.WPPR.Universal.Tournaments.Related.RelatedTournament>> GetRelatedTournaments(int tournamentId, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"RelatedTournaments:{tournamentId}",
                () => onlineApi.GetRelatedTournaments(tournamentId, cancellationToken));

        public Task<List<EventsByYearStatistics>> GetEventsByYearStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"EventsByYearStats:{playerSystem}",
                () => onlineApi.GetEventsByYearStatistics(playerSystem, cancellationToken));

        public Task<List<LargestTournamentStatistics>> GetLargestTournamentStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"LargestTournamentStats:{playerSystem}",
                () => onlineApi.GetLargestTournamentStatistics(playerSystem, cancellationToken));

        public Task<List<League>> GetLeagues(LeagueTimePeriod timePeriod, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"Leagues:{timePeriod}",
                () => onlineApi.GetLeagues(timePeriod, cancellationToken));

        public Task<List<LucrativeTournamentStatistics>> GetLucrativeTournamentStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"LucrativeTournamentStats:{playerSystem}",
                () => onlineApi.GetLucrativeTournamentStatistics(playerSystem, cancellationToken));

        public Task<OverallStatistics> GetOverallStatistics(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"OverallStatistics",
                () => onlineApi.GetOverallStatistics(cancellationToken));

        public Task<Player> GetPlayer(int playerId, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"Player:{playerId}",
                () => onlineApi.GetPlayer(playerId, cancellationToken));

        public Task<PlayerHistory> GetPlayerHistory(int playerId, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, bool activeResultsOnly = false, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayerHistory:{playerId}:{playerSystem}:{activeResultsOnly}",
                () => onlineApi.GetPlayerHistory(playerId, playerSystem, activeResultsOnly, cancellationToken));

        public Task<PlayerResults> GetPlayerResults(int playerId, PlayerRankingSystem rankingSystem = PlayerRankingSystem.Main, ResultType resultType = ResultType.Active, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayerResults:{playerId}:{rankingSystem}:{resultType}",
                () => onlineApi.GetPlayerResults(playerId, rankingSystem, resultType, cancellationToken));

        public Task<List<Player>> GetPlayers(List<int> playerIds, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"Players:{string.Join(",", playerIds)}",
                () => onlineApi.GetPlayers(playerIds, cancellationToken));

        public Task<List<PlayersByCountryStatistics>> GetPlayersByCountryStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayersByCountryStats:{playerSystem}",
                () => onlineApi.GetPlayersByCountryStatistics(playerSystem, cancellationToken));

        public Task<List<PlayersByStateStatistics>> GetPlayersByStateStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayersByStateStats:{playerSystem}",
                () => onlineApi.GetPlayersByStateStatistics(playerSystem, cancellationToken));

        public Task<List<PlayersByYearStatistics>> GetPlayersByYearStatistics(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayersByYearStats",
                () => onlineApi.GetPlayersByYearStatistics(cancellationToken));

        public Task<List<PlayersEventsAttendedByGivenPeriodStatistics>> GetPlayersEventsAttendedByGivenPeriod(DateOnly startDate, DateOnly endDate, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, int limit = 25, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayersEvents:{startDate:o}:{endDate:o}:{playerSystem}:{limit}",
                () => onlineApi.GetPlayersEventsAttendedByGivenPeriod(startDate, endDate, playerSystem, limit, cancellationToken));

        public Task<List<PlayersPointsByGivenPeriodStatistics>> GetPlayersPointsByGivenPeriod(DateOnly startDate, DateOnly endDate, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, int limit = 25, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayersPoints:{startDate:o}:{endDate:o}:{playerSystem}:{limit}",
                () => onlineApi.GetPlayersPointsByGivenPeriod(startDate, endDate, playerSystem, limit, cancellationToken));

        public Task<PlayerVersusPlayer> GetPlayerVersusPlayer(int playerId, PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayerVersusPlayer:{playerId}:{playerSystem}",
                () => onlineApi.GetPlayerVersusPlayer(playerId, playerSystem, cancellationToken));

        public Task<PlayerVersusPlayerComparison> GetPlayerVersusPlayerComparison(int playerId, int comparisonPlayerId, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayerVersusComparison:{playerId}:{comparisonPlayerId}",
                () => onlineApi.GetPlayerVersusPlayerComparison(playerId, comparisonPlayerId, cancellationToken));

        public Task<RankingCountries> GetRankingCountries(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"RankingCountries",
                () => onlineApi.GetRankingCountries(cancellationToken));

        public Task<List<RegionRepresentative>> GetRegionReps(string seriesCode, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"RegionReps:{seriesCode}",
                () => onlineApi.GetRegionReps(seriesCode, cancellationToken));

        public Task<List<PinballApi.Models.WPPR.Universal.Series.Region>> GetRegions(string seriesCode, int year, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"Regions:{seriesCode}:{year}",
                () => onlineApi.GetRegions(seriesCode, year, cancellationToken));

        public Task<List<Series>> GetSeries(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"Series",
                () => onlineApi.GetSeries(cancellationToken));

        public Task<SeriesOverallResults> GetSeriesOverallStanding(string seriesCode, int? year = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"SeriesOverall:{seriesCode}:{year}",
                () => onlineApi.GetSeriesOverallStanding(seriesCode, year, cancellationToken));

        public Task<SeriesPlayerCard> GetSeriesPlayerCard(int playerId, string seriesCode, string region, int? year = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"SeriesPlayerCard:{playerId}:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesPlayerCard(playerId, seriesCode, region, year, cancellationToken));

        public Task<RegionStandings> GetSeriesStandingsForRegion(string seriesCode, string region, int? year = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"SeriesStandings:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesStandingsForRegion(seriesCode, region, year, cancellationToken));

        public Task<SeriesStats> GetSeriesStats(string seriesCode, string region, int? year = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"SeriesStats:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesStats(seriesCode, region, year, cancellationToken));

        public Task<SeriesTournaments> GetSeriesTournamentsForRegion(string seriesCode, string region, int? year = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"SeriesTournaments:{seriesCode}:{region}:{year}",
                () => onlineApi.GetSeriesTournamentsForRegion(seriesCode, region, year, cancellationToken));

        public Task<SeriesWinners> GetSeriesWinners(string seriesCode, string region = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"SeriesWinners:{seriesCode}:{region}",
                () => onlineApi.GetSeriesWinners(seriesCode, region, cancellationToken));

        public Task<PinballApi.Models.WPPR.Universal.Tournaments.Tournament> GetTournament(int tournamentId, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"Tournament:{tournamentId}",
                () => onlineApi.GetTournament(tournamentId, cancellationToken));

        public Task<TournamentFormats> GetTournamentFormats(CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"TournamentFormats",
                () => onlineApi.GetTournamentFormats(cancellationToken));

        public Task<TournamentResults> GetTournamentResults(int tournamentId, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"TournamentResults:{tournamentId}",
                () => onlineApi.GetTournamentResults(tournamentId, cancellationToken));

        public Task<List<TournamentsByStateStatistics>> GetTournamentsByStateStatistics(PlayerRankingSystem playerSystem = PlayerRankingSystem.Main, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"TournamentsByStateStats:{playerSystem}",
                () => onlineApi.GetTournamentsByStateStatistics(playerSystem, cancellationToken));

        public Task<PlayerSearch> PlayerSearch(string name = null, string country = null, string stateProv = null, string tournamentName = null, int? tournamentPosition = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"PlayerSearch:{name}:{country}:{stateProv}:{tournamentName}:{tournamentPosition}",
                () => onlineApi.PlayerSearch(name, country, stateProv, tournamentName, tournamentPosition, cancellationToken));

        public Task<ProRankingSearch> ProRankingSearch(TournamentType rankingSystem, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"ProRankingSearch:{rankingSystem}",
                () => onlineApi.ProRankingSearch(rankingSystem, cancellationToken));

        public Task<RankingSearch> RankingSearch(RankingType rankingType, RankingSystem rankingSystem = RankingSystem.Open, int count = 100, int startPosition = 1, string countryCode = null, CancellationToken cancellationToken = default) =>
            ExecuteWithCache(
                $"RankingSearch:{rankingType}:{rankingSystem}:{count}:{startPosition}:{countryCode}",
                () => onlineApi.RankingSearch(rankingType, rankingSystem, count, startPosition, countryCode, cancellationToken));

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
            TournamentEventType? tournamentEventType = null,
            CancellationToken cancellationToken = default)
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
                maximumPoints, pointFilter, tournamentEventType, cancellationToken));
        }
    }
}

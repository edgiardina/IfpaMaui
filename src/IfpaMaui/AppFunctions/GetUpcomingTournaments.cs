using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Tournaments.Search;
using Shiny.AppFunctions;

namespace Ifpa.AppFunctions;

[AppFunction("get_upcoming_tournaments",
    Title = "Upcoming Tournaments",
    Description = "Gets the next IFPA pinball tournaments near the location set in the Calendar tab of the app")]
[AppShortcut("Upcoming ${applicationName} tournaments", ShortTitle = "Upcoming", SystemImage = "calendar")]
[AppShortcut("${applicationName} tournaments near me")]
[AppShortcut("Next ${applicationName} tournament")]
[AppShortcut("When is the next ${applicationName} tournament")]
public record GetUpcomingTournaments() : IAppFunction<UpcomingTournamentsResult>;

public record UpcomingTournamentsResult(string Location, int DistanceMiles, IReadOnlyList<UpcomingTournament> Tournaments);

public record UpcomingTournament(string Name, string Date, string City, string StateProvince);

public class GetUpcomingTournamentsHandler(
    IPinballRankingApi pinballRankingApi,
    IGeocoding geocoding,
    PendingAssistantRoute pendingRoute,
    ILogger<GetUpcomingTournamentsHandler> logger)
    : IAppFunctionHandler<GetUpcomingTournaments, UpcomingTournamentsResult>
{
    // Siri speaks the answer. More tournaments than this make the answer too long.
    const int MaxSpokenTournaments = 3;
    const int MaxTournaments = 10;

    // An assistant can start the app with no UI. Do not touch Shell or pages here.
    public async Task<UpcomingTournamentsResult> Handle(GetUpcomingTournaments request, AppFunctionContext context, CancellationToken cancellationToken)
    {
        // The same filter as the Calendar tab and the calendar widget.
        var location = Settings.LastCalendarLocation;
        var distance = Settings.LastCalendarDistance;

        try
        {
            var geoLocation = (await geocoding.GetLocationsAsync(location))?.FirstOrDefault();

            if (geoLocation == null)
                throw new AppFunctionException(AppFunctionErrorCode.NotFound, string.Format(Strings.AppFunctions_Tournaments_NoLocation, location));

            var tournamentType = (TournamentType?)(Settings.CalendarRankingSystem == "All" ? null : Enum.Parse(typeof(TournamentType), Settings.CalendarRankingSystem));
            TournamentEventType? eventType = Settings.CalendarShowLeagues ? null : TournamentEventType.Tournament;

            var items = await pinballRankingApi.TournamentSearch(geoLocation.Latitude,
                                                                 geoLocation.Longitude,
                                                                 distance, DistanceType.Miles,
                                                                 startDate: DateTime.Now,
                                                                 endDate: DateTime.Now.AddYears(1),
                                                                 tournamentType: tournamentType,
                                                                 tournamentEventType: eventType,
                                                                 totalReturn: 500);

            var tournaments = (items?.Tournaments ?? [])
                .OrderBy(n => n.EventStartDate)
                .Take(MaxTournaments)
                .ToList();

            var result = new UpcomingTournamentsResult(
                location,
                distance,
                tournaments.Select(n => new UpcomingTournament(n.TournamentName, n.EventStartDate.ToString("yyyy-MM-dd"), n.City, n.Stateprov)).ToList());

            // The app is in the background. If the user opens it from the answer, show the Calendar tab.
            if (!context.IsForeground)
                pendingRoute.Set("calendar");

            if (tournaments.Count == 0)
            {
                context.Say(string.Format(Strings.AppFunctions_Tournaments_None, distance, location));
            }
            else
            {
                var spoken = tournaments
                    .Take(MaxSpokenTournaments)
                    .Select(n => string.Format(Strings.AppFunctions_Tournaments_Item, n.TournamentName, DescribeDate(n.EventStartDate.Date), n.City));

                context.Say(string.Format(Strings.AppFunctions_Tournaments_Dialog, distance, location, string.Join("; ", spoken)));
            }

            return result;
        }
        catch (AppFunctionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading tournaments within {Distance} miles of {Location}", distance, location);
            throw new AppFunctionException(AppFunctionErrorCode.AppError, Strings.AppFunctions_Tournaments_Error, ex);
        }
    }

    static string DescribeDate(DateTime date)
    {
        if (date == DateTime.Today)
            return Strings.AppFunctions_Today;

        if (date == DateTime.Today.AddDays(1))
            return Strings.AppFunctions_Tomorrow;

        return date.ToString("dddd, MMMM d");
    }
}

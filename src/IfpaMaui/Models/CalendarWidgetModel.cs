using System.Globalization;

namespace Ifpa.Models
{
    /// <summary>
    /// A tournament as the calendar widget shows it. Days are dates with no
    /// time, so an event never slides to another day across time zones.
    /// </summary>
    public record WidgetTournament(long Id, string Name, string City, string Stateprov, DateTime StartDay, DateTime EndDay)
    {
        public string Place => string.Join(", ", new[] { City, Stateprov }.Where(n => !string.IsNullOrEmpty(n)));

        /// <summary>
        /// Opens the tournament through DeepLinkService, the same as a shared
        /// tournament link.
        /// </summary>
        public string Url => $"https://www.ifpapinball.com/tournaments/view.php?t={Id}";
    }

    /// <summary>
    /// The platform-independent part of the Android calendar widget: the month
    /// grid, which days have an event, and the date text.
    /// </summary>
    public static class CalendarWidgetModel
    {
        /// <summary>Opens the Calendar tab through DeepLinkService.</summary>
        public const string CalendarTabUrl = "https://www.ifpapinball.com/calendar";

        /// <summary>
        /// The weeks of the month that contains <paramref name="today"/>, seven
        /// cells each, padded with null before the first day and after the last.
        /// </summary>
        public static IReadOnlyList<DateTime?[]> Weeks(DateTime today, DayOfWeek firstDayOfWeek)
        {
            var first = new DateTime(today.Year, today.Month, 1);
            var dayCount = DateTime.DaysInMonth(today.Year, today.Month);
            var leading = ((int)first.DayOfWeek - (int)firstDayOfWeek + 7) % 7;

            var cells = new List<DateTime?>();
            cells.AddRange(Enumerable.Repeat<DateTime?>(null, leading));
            cells.AddRange(Enumerable.Range(0, dayCount).Select(n => (DateTime?)first.AddDays(n)));
            while (cells.Count % 7 != 0)
            {
                cells.Add(null);
            }

            return cells.Chunk(7).ToList();
        }

        /// <summary>
        /// Narrow weekday names in grid order, starting at the culture's first
        /// day of the week.
        /// </summary>
        public static IReadOnlyList<string> WeekdaySymbols(CultureInfo culture)
        {
            var format = culture.DateTimeFormat;
            var first = (int)format.FirstDayOfWeek;
            return Enumerable.Range(0, 7)
                .Select(n => format.ShortestDayNames[(first + n) % 7])
                .Select(name => name.Substring(0, 1).ToUpper(culture))
                .ToList();
        }

        /// <summary>
        /// Tournaments that have not ended by <paramref name="today"/>, soonest
        /// first.
        /// </summary>
        public static IReadOnlyList<WidgetTournament> Upcoming(IEnumerable<WidgetTournament> tournaments, DateTime today)
        {
            return tournaments
                .Where(n => n.EndDay.Date >= today.Date)
                .OrderBy(n => n.StartDay)
                .ThenBy(n => n.Name)
                .ToList();
        }

        /// <summary>Every day that an upcoming tournament covers.</summary>
        public static HashSet<DateTime> EventDays(IEnumerable<WidgetTournament> upcoming)
        {
            var days = new HashSet<DateTime>();
            foreach (var tournament in upcoming)
            {
                for (var day = tournament.StartDay.Date; day <= tournament.EndDay.Date; day = day.AddDays(1))
                {
                    days.Add(day);
                }
            }
            return days;
        }

        /// <summary>
        /// The place, led by the date range for a tournament of more than one
        /// day: "Sep 26 – 28 · Chicago, IL", or "Sep 30 – Oct 2" across months.
        /// </summary>
        public static string Detail(WidgetTournament tournament, CultureInfo culture)
        {
            var parts = new List<string>();

            if (tournament.EndDay.Date > tournament.StartDay.Date)
            {
                var start = tournament.StartDay.ToString("MMM d", culture);
                var end = tournament.EndDay.Month == tournament.StartDay.Month
                    ? tournament.EndDay.ToString("%d", culture)
                    : tournament.EndDay.ToString("MMM d", culture);
                parts.Add($"{start} – {end}");
            }

            if (!string.IsNullOrEmpty(tournament.Place))
            {
                parts.Add(tournament.Place);
            }

            return string.Join(" · ", parts);
        }
    }
}

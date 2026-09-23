using System.Globalization;
using Ifpa.Models;
using Xunit;

namespace Ifpa.Tests
{
    // Covers the month grid, event days and date text behind the Android calendar widget.
    public class CalendarWidgetModelTests
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        private static WidgetTournament Tournament(DateTime start, DateTime end, string name = "Event", string city = "Chicago", string state = "IL")
            => new(1, name, city, state, start, end);

        [Fact]
        public void Weeks_pads_september_2026_from_a_sunday_start()
        {
            // 1 September 2026 is a Tuesday, and the month has 30 days.
            var weeks = CalendarWidgetModel.Weeks(new DateTime(2026, 9, 22), DayOfWeek.Sunday);

            Assert.Equal(5, weeks.Count);
            Assert.Null(weeks[0][0]);
            Assert.Null(weeks[0][1]);
            Assert.Equal(new DateTime(2026, 9, 1), weeks[0][2]);
            Assert.Equal(new DateTime(2026, 9, 30), weeks[4][3]);
            Assert.Null(weeks[4][6]);
        }

        [Fact]
        public void Weeks_follows_a_monday_start()
        {
            var weeks = CalendarWidgetModel.Weeks(new DateTime(2026, 9, 22), DayOfWeek.Monday);

            Assert.Null(weeks[0][0]);
            Assert.Equal(new DateTime(2026, 9, 1), weeks[0][1]);
        }

        [Fact]
        public void Weeks_uses_six_rows_when_the_month_needs_them()
        {
            // 1 August 2026 is a Saturday, so 31 days span six weeks.
            var weeks = CalendarWidgetModel.Weeks(new DateTime(2026, 8, 15), DayOfWeek.Sunday);

            Assert.Equal(6, weeks.Count);
            Assert.All(weeks, week => Assert.Equal(7, week.Length));
            Assert.Equal(new DateTime(2026, 8, 31), weeks[5][1]);
        }

        [Fact]
        public void WeekdaySymbols_start_at_the_culture_first_day()
        {
            Assert.Equal(new[] { "S", "M", "T", "W", "T", "F", "S" }, CalendarWidgetModel.WeekdaySymbols(English));
            Assert.Equal("M", CalendarWidgetModel.WeekdaySymbols(CultureInfo.GetCultureInfo("en-GB"))[0]);
        }

        [Fact]
        public void Upcoming_drops_ended_events_and_keeps_one_still_running()
        {
            var today = new DateTime(2026, 9, 22);
            var ended = Tournament(new DateTime(2026, 9, 20), new DateTime(2026, 9, 21), "Ended");
            var running = Tournament(new DateTime(2026, 9, 21), new DateTime(2026, 9, 23), "Running");
            var later = Tournament(new DateTime(2026, 9, 26), new DateTime(2026, 9, 26), "Later");

            var upcoming = CalendarWidgetModel.Upcoming(new[] { later, ended, running }, today);

            Assert.Equal(new[] { "Running", "Later" }, upcoming.Select(n => n.Name));
        }

        [Fact]
        public void EventDays_covers_every_day_of_a_multi_day_event()
        {
            var days = CalendarWidgetModel.EventDays(new[]
            {
                Tournament(new DateTime(2026, 9, 26), new DateTime(2026, 9, 28)),
                Tournament(new DateTime(2026, 9, 24), new DateTime(2026, 9, 24)),
            });

            Assert.Equal(4, days.Count);
            Assert.Contains(new DateTime(2026, 9, 27), days);
            Assert.DoesNotContain(new DateTime(2026, 9, 25), days);
        }

        [Fact]
        public void Detail_is_the_place_for_a_one_day_event()
        {
            var detail = CalendarWidgetModel.Detail(Tournament(new DateTime(2026, 9, 24), new DateTime(2026, 9, 24)), English);

            Assert.Equal("Chicago, IL", detail);
        }

        [Fact]
        public void Detail_leads_with_the_range_for_a_multi_day_event()
        {
            var sameMonth = Tournament(new DateTime(2026, 9, 26), new DateTime(2026, 9, 28));
            var acrossMonths = Tournament(new DateTime(2026, 9, 30), new DateTime(2026, 10, 2), city: null, state: null);

            Assert.Equal("Sep 26 – 28 · Chicago, IL", CalendarWidgetModel.Detail(sameMonth, English));
            Assert.Equal("Sep 30 – Oct 2", CalendarWidgetModel.Detail(acrossMonths, English));
        }

        [Fact]
        public void Url_opens_the_tournament_through_the_shared_link()
        {
            var tournament = new WidgetTournament(12345, "Event", null, null, DateTime.Today, DateTime.Today);

            Assert.Equal("https://www.ifpapinball.com/tournaments/view.php?t=12345", tournament.Url);
        }
    }
}

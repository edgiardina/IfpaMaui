using Ifpa.Models;
using Xunit;

namespace Ifpa.Tests
{
    // Covers the small int -> TimeSpan/DateTime helpers used throughout the app for cache TTLs,
    // polling intervals and "x minutes ago" style calculations.
    public class IntExtensionsTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(45)]
        [InlineData(3600)]
        public void Seconds_returns_a_timespan_of_that_many_seconds(int n)
            => Assert.Equal(TimeSpan.FromSeconds(n), n.Seconds());

        [Theory]
        [InlineData(1)]
        [InlineData(90)]
        public void Minutes_returns_a_timespan_of_that_many_minutes(int n)
            => Assert.Equal(TimeSpan.FromMinutes(n), n.Minutes());

        [Theory]
        [InlineData(1)]
        [InlineData(24)]
        public void Hours_returns_a_timespan_of_that_many_hours(int n)
            => Assert.Equal(TimeSpan.FromHours(n), n.Hours());

        [Theory]
        [InlineData(1)]
        [InlineData(30)]
        public void Days_returns_a_timespan_of_that_many_days(int n)
            => Assert.Equal(TimeSpan.FromDays(n), n.Days());

        [Fact]
        public void Milliseconds_returns_a_timespan_of_that_many_milliseconds()
            => Assert.Equal(TimeSpan.FromMilliseconds(250), 250.Milliseconds());

        [Fact]
        public void Singular_aliases_match_their_plural_forms()
        {
            Assert.Equal(1.Seconds(), 1.Second());
            Assert.Equal(1.Minutes(), 1.Minute());
            Assert.Equal(1.Hours(), 1.Hour());
            Assert.Equal(1.Days(), 1.Day());
        }

        [Fact]
        public void MinutesFromNow_returns_a_time_in_the_future()
        {
            var before = DateTime.UtcNow;
            var result = 10.MinutesFromNow();

            Assert.InRange(result,
                before.AddMinutes(10).AddSeconds(-5),
                DateTime.UtcNow.AddMinutes(10).AddSeconds(5));
        }

        [Fact]
        public void MinutesAgo_returns_a_time_in_the_past()
            => Assert.True(10.MinutesAgo() < DateTime.UtcNow);
    }
}

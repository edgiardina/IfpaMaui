using System.Globalization;
using Ifpa.Models;
using Xunit;

namespace Ifpa.Tests
{
    // Covers how the Android widgets show IFPA numbers.
    public class WidgetFormatTests
    {
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        [Theory]
        [InlineData(1L, "1st")]
        [InlineData(2L, "2nd")]
        [InlineData(3L, "3rd")]
        [InlineData(4L, "4th")]
        [InlineData(11L, "11th")]
        [InlineData(12L, "12th")]
        [InlineData(13L, "13th")]
        [InlineData(21L, "21st")]
        [InlineData(112L, "112th")]
        [InlineData(1001L, "1001st")]
        public void Ordinal_uses_the_english_suffix(long value, string expected)
            => Assert.Equal(expected, WidgetFormat.Ordinal(value));

        [Theory]
        [InlineData(null)]
        [InlineData(0L)]
        public void Ordinal_shows_a_dash_for_no_rank(long? value)
            => Assert.Equal(WidgetFormat.Missing, WidgetFormat.Ordinal(value));

        [Fact]
        public void Points_groups_and_trims_the_decimals()
        {
            Assert.Equal("1,024.61", WidgetFormat.Points(1024.6100, English));
            Assert.Equal("54.5", WidgetFormat.Points(54.500, English));
            Assert.Equal("12", WidgetFormat.Points(12, English));
            Assert.Equal(WidgetFormat.Missing, WidgetFormat.Points(null, English));
        }

        [Fact]
        public void Percent_keeps_one_decimal()
        {
            Assert.Equal("54.5%", WidgetFormat.Percent(54.500, English));
            Assert.Equal("60%", WidgetFormat.Percent(60, English));
            Assert.Equal(WidgetFormat.Missing, WidgetFormat.Percent(null, English));
        }

        [Fact]
        public void Count_groups_thousands()
            => Assert.Equal("12,345", WidgetFormat.Count(12345, English));
    }
}

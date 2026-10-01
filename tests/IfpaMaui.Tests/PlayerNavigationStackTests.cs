using Ifpa.Models;
using Xunit;

namespace Ifpa.Tests
{
    // Covers the stack lookup used to avoid pushing a second details page for a player who is
    // already in the navigation stack (e.g. player -> results -> tournament -> same player).
    public class PlayerNavigationStackTests
    {
        [Fact]
        public void Returns_null_when_player_is_not_in_the_stack()
            => Assert.Null(PlayerNavigationStack.PagesToPopTo(new int?[] { null, 10, null }, 42));

        [Fact]
        public void Returns_null_for_an_empty_stack()
            => Assert.Null(PlayerNavigationStack.PagesToPopTo(new int?[0], 42));

        [Fact]
        public void Returns_zero_when_player_is_the_current_page()
            => Assert.Equal(0, PlayerNavigationStack.PagesToPopTo(new int?[] { null, 42 }, 42));

        [Fact]
        public void Returns_pages_above_the_existing_player_page()
            => Assert.Equal(3, PlayerNavigationStack.PagesToPopTo(new int?[] { null, 42, null, null, null }, 42));

        [Fact]
        public void Matches_the_root_page()
            => Assert.Equal(2, PlayerNavigationStack.PagesToPopTo(new int?[] { 42, null, null }, 42));

        [Fact]
        public void Uses_the_topmost_match_when_player_appears_more_than_once()
            => Assert.Equal(1, PlayerNavigationStack.PagesToPopTo(new int?[] { 42, null, 42, null }, 42));
    }
}

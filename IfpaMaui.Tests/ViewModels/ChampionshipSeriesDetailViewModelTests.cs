using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Series;
using Ifpa.ViewModels;
using System.Threading.Tasks;

namespace IfpaMaui.Tests
{
    public class ChampionshipSeriesDetailViewModelTests
    {
        private Mock<IPinballRankingApi> _pinballRankingApiMock;
        private Mock<ILogger<ChampionshipSeriesDetailViewModel>> _loggerMock;
        private ChampionshipSeriesDetailViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _pinballRankingApiMock = new Mock<IPinballRankingApi>();
            _loggerMock = new Mock<ILogger<ChampionshipSeriesDetailViewModel>>();
            _viewModel = new ChampionshipSeriesDetailViewModel(_pinballRankingApiMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task LoadItems_ShouldPopulateViewModelProperties()
        {
            // Arrange
            var regionStandings = new RegionStandings();
            var seriesTournaments = new SeriesTournaments();

            _pinballRankingApiMock.Setup(api => api.GetSeriesStandingsForRegion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                                  .ReturnsAsync(regionStandings);
            _pinballRankingApiMock.Setup(api => api.GetSeriesTournamentsForRegion(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                                  .ReturnsAsync(seriesTournaments);

            _viewModel.RegionCode = "Region";
            _viewModel.SeriesCode = "Series";
            _viewModel.Year = 2023;

            // Act
            await _viewModel.LoadItems();

            // Assert
            Assert.AreEqual(regionStandings, _viewModel.RegionStandings);
            Assert.AreEqual(seriesTournaments, _viewModel.SeriesTournaments);
        }

        [Test]
        public async Task SelectRegionStandings_ShouldNavigateToRegionStandings()
        {
            // Arrange
            var regionStanding = new RegionStanding { PlayerId = 1 };
            _viewModel.SelectedRegionStandings = regionStanding;

            // Act
            await _viewModel.SelectRegionStandings();

            // Assert
            Assert.IsNull(_viewModel.SelectedRegionStandings);
        }

        [Test]
        public async Task SelectTournament_ShouldNavigateToTournamentDetails()
        {
            // Arrange
            var tournament = new SubmittedTournament { TournamentId = 1 };
            _viewModel.SelectedTournament = tournament;

            // Act
            await _viewModel.SelectTournament();

            // Assert
            Assert.IsNull(_viewModel.SelectedTournament);
        }
    }
}

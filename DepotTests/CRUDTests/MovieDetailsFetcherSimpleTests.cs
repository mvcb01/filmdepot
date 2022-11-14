using System.Linq;
using Moq;
using Xunit;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Threading.Tasks;

using FilmCRUD;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;
using FilmCRUD.Interfaces;
using ConfigUtils.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DepotTests.CRUDTests
{
    public class MovieDetailsFetcherSimpleTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IRateLimitPolicyConfig> _rateLimitConfigMock;

        private readonly Mock<IRetryPolicyConfig> _retryConfigMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieDetailsFetcherSimple _movieDetailsFetcherSimple;

        public MovieDetailsFetcherSimpleTests()
        {
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);

            this._rateLimitConfigMock = new Mock<IRateLimitPolicyConfig>();
            this._retryConfigMock = new Mock<IRetryPolicyConfig>();

            // default policy configs
            this._rateLimitConfigMock.SetupGet(pol => pol.NumberOfExecutions).Returns(5);
            this._rateLimitConfigMock.SetupGet(pol => pol.PerTimeSpan).Returns(TimeSpan.FromMilliseconds(50));

            this._retryConfigMock.SetupGet(pol => pol.RetryCount).Returns(2);
            this._retryConfigMock.SetupGet(pol => pol.SleepDuration).Returns(TimeSpan.FromMilliseconds(50));

            this._appSettingsManagerMock = new Mock<IAppSettingsManager>();

            this._appSettingsManagerMock.Setup(a => a.GetRateLimitPolicyConfig()).Returns(this._rateLimitConfigMock.Object);
            this._appSettingsManagerMock.Setup(a => a.GetRetryPolicyConfig()).Returns(this._retryConfigMock.Object);

            this._movieAPIClientMock = new Mock<IMovieAPIClient>();

            this._movieDetailsFetcherSimple = new MovieDetailsFetcherSimple(
                this._unitOfWorkMock.Object,
                this._appSettingsManagerMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public async Task PopulateMovieKeywordsAsync_WithoutMoviesMissingKeywords_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutKeywords())
                .Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieKeywordsAsync();

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieKeywordsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PopulateMovieKeywordsAsync_WithMoviesMissingKeywords_ShouldCallApiClient()
        {
            // arrange
            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovie = new Movie() { Title = "total recall", ReleaseDate = 1989, ExternalId = firstExternalId };
            var secondMovie = new Movie() { Title = "get carter", ReleaseDate = 1971, ExternalId = secondExternalId };
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutKeywords())
                .Returns(new Movie[] { firstMovie, secondMovie });

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieKeywordsAsync();

            // assert
            this._movieAPIClientMock.Verify(
                m => m.GetMovieKeywordsAsync(It.Is<int>( i => i == firstExternalId | i == secondExternalId)),
                Times.Exactly(2));
        }

        [Fact]
        public async Task PopulateMovieKeywordsAsync_WithMoviesMissingKeywords_ShouldPopulateKeywordsCorrectly()
        {
            // arrange
            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutKeywords = new Movie() {
                Title = "total recall",
                ReleaseDate = 1989,
                ExternalId = firstExternalId };
            var secondMovieWithoutKeywords = new Movie() {
                Title = "get carter",
                ReleaseDate = 1971,
                ExternalId = secondExternalId };

            var firstMovieKeywords = new string[] { "planet mars", "oxygen"};
            var secondMovieKeywords = new string[] { "hitman", "northern england", "loss of loved one"};
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutKeywords())
                .Returns(new Movie[] { firstMovieWithoutKeywords, secondMovieWithoutKeywords });

            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieKeywordsAsync(firstExternalId))
                .ReturnsAsync(firstMovieKeywords);
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieKeywordsAsync(secondExternalId))
                .ReturnsAsync(secondMovieKeywords);

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieKeywordsAsync();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutKeywords.Keywords.Should().BeEquivalentTo(firstMovieKeywords);
                secondMovieWithoutKeywords.Keywords.Should().BeEquivalentTo(secondMovieKeywords);
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task PopulateMovieKeywordsAsync_WithLimitOnNumberOfApiCalls_ShouldNotExceedLimit(int maxApiCalls)
        {
            // arrange
            var firstMovie = new Movie() { Title = "My Cousin Vinny", ReleaseDate = 1992, ExternalId = 101, Keywords = new List<string>() };
            var secondMovie = new Movie() { Title = "Payback", ReleaseDate = 1999, ExternalId = 102, Keywords = new List<string>() };
            var thirdMovie = new Movie() { Title = "Office Space", ReleaseDate = 1999, ExternalId = 103, Keywords = new List<string>() };

            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutKeywords())
                .Returns(new[] { firstMovie, secondMovie, thirdMovie });

            this._movieAPIClientMock
                .Setup(m => m.GetMovieKeywordsAsync(It.IsAny<int>()))
                .ReturnsAsync(new string[] { "dummy keyword" });

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieKeywordsAsync(maxApiCalls);

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieKeywordsAsync(It.IsAny<int>()), Times.Exactly(maxApiCalls));
        }

        [Fact]
        public async Task PopulateMovieIMDBIdsAsync_WithoutMoviesMissingIMDBIds_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutImdbId())
                .Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieIMDBIdsAsync();

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieIMDBIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PopulateMovieIMDBIdsAsync_WithMoviesMissingIMDBIds_ShouldCallApiClient()
        {
            // arrange
            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovie = new Movie() { Title = "total recall", ReleaseDate = 1989, ExternalId = firstExternalId };
            var secondMovie = new Movie() { Title = "get carter", ReleaseDate = 1971, ExternalId = secondExternalId };
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutImdbId())
                .Returns(new Movie[] { firstMovie, secondMovie });

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieIMDBIdsAsync();

            // assert
            this._movieAPIClientMock.Verify(
                m => m.GetMovieIMDBIdAsync(It.Is<int>(i => i == firstExternalId || i == secondExternalId)),
                Times.Exactly(2));
        }

        [Fact]
        public async Task PopulateMovieIMDBIdsAsync_WithMoviesMissingIMDBIds_ShouldPopulateIMDBIdsCorrectly()
        {
            // arrange
            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutIMDBId = new Movie() {
                Title = "total recall",
                ReleaseDate = 1989,
                ExternalId = firstExternalId };
            var secondMovieWithoutIMDBId = new Movie() {
                Title = "get carter",
                ReleaseDate = 1971,
                ExternalId = secondExternalId };

            var firstMovieImdbId = "tt001";
            var secondMovieImdbId = "tt002";
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutImdbId())
                .Returns(new Movie[] { firstMovieWithoutIMDBId, secondMovieWithoutIMDBId });
            this._movieAPIClientMock
                .Setup(m => m.GetMovieIMDBIdAsync(firstExternalId))
                .ReturnsAsync(firstMovieImdbId);
            this._movieAPIClientMock
                .Setup(m => m.GetMovieIMDBIdAsync(secondExternalId))
                .ReturnsAsync(secondMovieImdbId);

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieIMDBIdsAsync();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutIMDBId.IMDBId.Should().Be(firstMovieImdbId);
                secondMovieWithoutIMDBId.IMDBId.Should().Be(secondMovieImdbId);
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task PopulateMovieIMDBIdsAsync_WithLimitOnNumberOfApiCalls_ShouldNotExceedLimit(int maxApiCalls)
        {
            // arrange
            var firstMovie = new Movie() { Title = "My Cousin Vinny", ReleaseDate = 1992, ExternalId = 101 };
            var secondMovie = new Movie() { Title = "Payback", ReleaseDate = 1999, ExternalId = 102 };
            var thirdMovie = new Movie() { Title = "Office Space", ReleaseDate = 1999, ExternalId = 103 };

            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutImdbId())
                .Returns(new[] { firstMovie, secondMovie, thirdMovie });

            this._movieAPIClientMock
                .Setup(m => m.GetMovieIMDBIdAsync(It.IsAny<int>()))
                .ReturnsAsync("tt00112233");

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieIMDBIdsAsync(maxApiCalls);

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieIMDBIdAsync(It.IsAny<int>()), Times.Exactly(maxApiCalls));
        }
    }
}
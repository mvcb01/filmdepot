using System.Linq;
using Moq;
using Xunit;
using FluentAssertions;
using FluentAssertions.Execution;

using FilmCRUD;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;

namespace DepotTests.CRUDTests
{
    public class MovieDetailsFetcherSimpleTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieDetailsFetcherSimple _movieDetailsFetcherSimple;

        public MovieDetailsFetcherSimpleTests()
        {
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);

            this._movieAPIClientMock = new Mock<IMovieAPIClient>();

            this._movieDetailsFetcherSimple = new MovieDetailsFetcherSimple(
                this._unitOfWorkMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public async void GetKeywordsForMovies_WithoutMoviesMissingKeywords_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutKeywords())
                .Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherSimple.PopulateMovieKeywords();

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieKeywordsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async void GetKeywordsForMovies_WithMoviesMissingKeywords_ShouldCallApiClient()
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
            await this._movieDetailsFetcherSimple.PopulateMovieKeywords();

            // assert
            this._movieAPIClientMock.Verify(
                m => m.GetMovieKeywordsAsync(It.Is<int>( i => i == firstExternalId | i == secondExternalId)),
                Times.Exactly(2));
        }

        [Fact]
        public async void GetKeywordsForMovies_WithMoviesMissingKeywords_ShouldPopulateKeywordsCorrectly()
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
            await this._movieDetailsFetcherSimple.PopulateMovieKeywords();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutKeywords.Keywords.Should().BeEquivalentTo(firstMovieKeywords);
                secondMovieWithoutKeywords.Keywords.Should().BeEquivalentTo(secondMovieKeywords);
            }
        }


    }
}
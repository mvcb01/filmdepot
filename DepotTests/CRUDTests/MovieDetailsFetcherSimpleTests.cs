using System.Linq;
using Moq;
using Xunit;
using FluentAssertions;

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
        public async void GetKeywordsForMovies_WithMoviesMissingKeywords_ShouldNotCallApiClient()
        {
            // arrange
            // var firstMovie = new Movie() { Title = "total recall", ReleaseDate = 1989 };
            // var secondMovie = new Movie() { Title = "get carter", ReleaseDate = 1971 };
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutKeywords())
                .Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherSimple.GetKeywordsForMovies();

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieKeywordsAsync(It.IsAny<int>()), Times.Never);
        }


    }
}
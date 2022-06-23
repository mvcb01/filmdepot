using Xunit;
using Moq;
using FluentAssertions;

using FilmCRUD;
using FilmDomain.Interfaces;
using FilmDomain.Entities;
using MovieAPIClients.Interfaces;

namespace DepotTests.CRUDTests
{
    public class MovieDetailsFetcherGenresTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IGenreRepository> _genreRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieDetailsFetcherGenres _movieDetailsFetcherGenres;

        public MovieDetailsFetcherGenresTests()
        {
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);
            this._genreRepositoryMock = new Mock<IGenreRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Genres)
                .Returns(this._genreRepositoryMock.Object);

            this._movieAPIClientMock = new Mock<IMovieAPIClient>(MockBehavior.Strict);

            this._movieDetailsFetcherGenres = new MovieDetailsFetcherGenres(
                this._unitOfWorkMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public void TestName()
        {
            // arrange


            // act

            // assert
        }

    }
}
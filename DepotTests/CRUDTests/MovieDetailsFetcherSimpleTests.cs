using Moq;
using FluentAssertions;

using FilmCRUD;
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
            this._movieRepositoryMock = new Mock<IMovieRepository>();

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);

            this._movieAPIClientMock = new Mock<IMovieAPIClient>();

            this._movieDetailsFetcherSimple = new MovieDetailsFetcherSimple(
                this._unitOfWorkMock.Object,
                this._movieAPIClientMock.Object);
        }


    }
}
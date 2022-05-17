using Xunit;
using Moq;
using FluentAssertions;

using FilmCRUD;
using FilmDomain.Interfaces;
using ConfigUtils.Interfaces;
using MovieAPIClients.Interfaces;

namespace DepotTests.CRUDTests
{
    public class RipToMovieLinkerTests
    {
        private readonly Mock<IMovieRipRepository> _movieRipRepositoryMock;

        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

        private readonly RipToMovieLinker _ripToMovieLinker;

        public RipToMovieLinkerTests()
        {
            this._movieAPIClientMock = new Mock<IMovieAPIClient>();
            this._appSettingsManagerMock = new Mock<IAppSettingsManager>();

            this._movieRipRepositoryMock = new Mock<IMovieRipRepository>();
            this._movieRepositoryMock = new Mock<IMovieRepository>();

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.MovieRips)
                .Returns(this._movieRipRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);

            this._ripToMovieLinker = new RipToMovieLinker(
                this._unitOfWorkMock.Object,
                this._appSettingsManagerMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public void TestMethod()
        {
        }
    }
}
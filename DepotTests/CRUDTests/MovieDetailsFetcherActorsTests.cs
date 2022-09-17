using Xunit;
using Moq;
using FluentAssertions;
using FluentAssertions.Execution;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

using FilmCRUD;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients;
using MovieAPIClients.Interfaces;
using FilmCRUD.Interfaces;

namespace DepotTests.CRUDTests
{
    public class MovieDetailsFetcherActorsTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IActorRepository> _actorRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IFileSystemIOWrapper> _fileSystemIOWrapper;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieDetailsFetcherActors _movieDetailsFetcherActors;

        public MovieDetailsFetcherActorsTests()
        {
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);
            this._actorRepositoryMock = new Mock<IActorRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Actors)
                .Returns(this._actorRepositoryMock.Object);

            this._fileSystemIOWrapper = new Mock<IFileSystemIOWrapper>();

            this._movieAPIClientMock = new Mock<IMovieAPIClient>();

            this._movieDetailsFetcherActors = new MovieDetailsFetcherActors(
                this._unitOfWorkMock.Object,
                this._fileSystemIOWrapper.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public async Task PopulateDetails_WithoutMoviesMissingActors_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRepositoryMock.Setup(m => m.GetMoviesWithoutActors()).Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherActors.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieActorsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PopulateDetails_WithMoviesMissingActors_ShouldCallMovieAPIClient()
        {
            // arrange
            int externalId = 101;
            var movieWithoutActors = new Movie() {
                Title = "the fly", ReleaseDate = 1986, ExternalId = externalId, Actors = new List<Actor>()
            };
            this._actorRepositoryMock
                .Setup(a => a.GetAll())
                .Returns(new List<Actor>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutActors())
                .Returns(new Movie[] { movieWithoutActors });
            this._movieAPIClientMock
                .Setup(m => m.GetMovieActorsAsync(It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Empty<MovieActorResult>());

            // act
            await this._movieDetailsFetcherActors.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieActorsAsync(externalId), Times.Once);
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingActors_WithExistingActorResultInRepo_ShouldBePopulatedWithExistingActor()
        {
            // arrange
            var actorResult = new MovieActorResult() { Name = "joaquin phoenix", ExternalId = 201 };
            var actor = (Actor)actorResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutActors = new Movie() {
                Title = "i'm still here", ReleaseDate = 2010, ExternalId = firstExternalId, Actors = new List<Actor>()
            };
            var secondMovieWithoutActors = new Movie() {
                Title = "joker", ReleaseDate = 2019, ExternalId = secondExternalId, Actors = new List<Actor>()
            };
            this._actorRepositoryMock
                .Setup(a => a.GetAll())
                .Returns(new List<Actor>() { actor });
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutActors())
                .Returns(new Movie[] { firstMovieWithoutActors, secondMovieWithoutActors });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieActorsAsync(It.Is<int>(i => i == firstExternalId | i == secondExternalId)))
                .ReturnsAsync(new MovieActorResult[] { actorResult });

            // act
            await this._movieDetailsFetcherActors.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutActors.Actors.FirstOrDefault().Should().BeSameAs(actor);
                secondMovieWithoutActors.Actors.FirstOrDefault().Should().BeSameAs(actor);
            }
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingActors_WithoutSuchActorsInRepo_ShouldBePopulatedWithNewActors()
        {
            // arrange
            var firstActorResult = new MovieActorResult() { Name = "joaquin phoenix", ExternalId = 201 };
            var firstActor = (Actor)firstActorResult;

            var secondActorResult = new MovieActorResult() { Name = "adrien brody", ExternalId = 202 };
            var secondActor = (Actor)secondActorResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutActors = new Movie() {
                Title = "i'm still here", ReleaseDate = 2010, ExternalId = firstExternalId, Actors = new List<Actor>()
            };
            var secondMovieWithoutActors = new Movie() {
                Title = "the village", ReleaseDate = 2004, ExternalId = secondExternalId, Actors = new List<Actor>()
            };

            this._actorRepositoryMock
                .Setup(a => a.GetAll())
                .Returns(Enumerable.Empty<Actor>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutActors())
                .Returns(new Movie[] { firstMovieWithoutActors, secondMovieWithoutActors });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieActorsAsync(firstExternalId))
                .ReturnsAsync(new MovieActorResult[] { firstActorResult });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieActorsAsync(secondExternalId))
                .ReturnsAsync(new MovieActorResult[] { firstActorResult, secondActorResult });

            // act
            await this._movieDetailsFetcherActors.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutActors.Actors.Should().BeEquivalentTo(new List<Actor>() { firstActor });
                secondMovieWithoutActors.Actors.Should().BeEquivalentTo(new List<Actor>() { firstActor, secondActor });
            }
        }
    }
}
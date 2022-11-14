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
using ConfigUtils.Interfaces;
using System;

namespace DepotTests.CRUDTests
{
    public class MovieDetailsFetcherActorsTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IActorRepository> _actorRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IRateLimitPolicyConfig> _rateLimitConfigMock;

        private readonly Mock<IRetryPolicyConfig> _retryConfigMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

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

            this._movieDetailsFetcherActors = new MovieDetailsFetcherActors(
                this._unitOfWorkMock.Object,
                this._appSettingsManagerMock.Object,
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
            var movieWithoutActors = new Movie()
            {
                Title = "the fly",
                ReleaseDate = 1986,
                ExternalId = externalId,
                Actors = new List<Actor>()
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
            var firstMovieWithoutActors = new Movie()
            {
                Title = "i'm still here",
                ReleaseDate = 2010,
                ExternalId = firstExternalId,
                Actors = new List<Actor>()
            };
            var secondMovieWithoutActors = new Movie()
            {
                Title = "joker",
                ReleaseDate = 2019,
                ExternalId = secondExternalId,
                Actors = new List<Actor>()
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
            var firstMovieWithoutActors = new Movie()
            {
                Title = "i'm still here",
                ReleaseDate = 2010,
                ExternalId = firstExternalId,
                Actors = new List<Actor>()
            };
            var secondMovieWithoutActors = new Movie()
            {
                Title = "the village",
                ReleaseDate = 2004,
                ExternalId = secondExternalId,
                Actors = new List<Actor>()
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

        [Fact]
        public async Task PopulateDetails_WithMoviesMissingActors_WithoutSuchActorsInRepo_WithSameActorForAllMovies_ShouldBePopulatedWithTheSameActorEntity()
        {
            // arrange
            var firstActorResult = new MovieActorResult() { Name = "joaquin phoenix", ExternalId = 201 };
            var secondActorResult = new MovieActorResult() { Name = "adrien brody", ExternalId = 202 };

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutActors = new Movie()
            {
                Title = "i'm still here",
                ReleaseDate = 2010,
                ExternalId = firstExternalId,
                Actors = new List<Actor>()
            };
            var secondMovieWithoutActors = new Movie()
            {
                Title = "the village",
                ReleaseDate = 2004,
                ExternalId = secondExternalId,
                Actors = new List<Actor>()
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
            firstMovieWithoutActors.Actors
                .First(a => a.ExternalId == firstActorResult.ExternalId)
                .Should()
                .BeSameAs(secondMovieWithoutActors.Actors.First(a => a.ExternalId == firstActorResult.ExternalId));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task PopulateDetails_WithLimitOnNumberOfApiCalls_ShouldNotExceedLimit(int maxApiCalls)
        {
            // arrange
            var firstMovie = new Movie() { Title = "My Cousin Vinny", ReleaseDate = 1992, ExternalId = 101, Actors = new List<Actor>() };
            var secondMovie = new Movie() { Title = "Payback", ReleaseDate = 1999, ExternalId = 102, Actors = new List<Actor>() };
            var thirdMovie = new Movie() { Title = "Office Space", ReleaseDate = 1999, ExternalId = 103, Actors = new List<Actor>() };

            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutActors())
                .Returns(new[] { firstMovie, secondMovie, thirdMovie });

            this._actorRepositoryMock.Setup(d => d.GetAll()).Returns(Enumerable.Empty<Actor>());

            this._movieAPIClientMock
                .Setup(m => m.GetMovieActorsAsync(It.IsAny<int>()))
                .ReturnsAsync(new MovieActorResult[] { new MovieActorResult() });

            // act
            await this._movieDetailsFetcherActors.PopulateDetails(maxApiCalls);

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieActorsAsync(It.IsAny<int>()), Times.Exactly(maxApiCalls));
        }

    }

}
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
using ConfigUtils.Interfaces;
using System;

namespace DepotTests.CRUDTests
{
    public class MovieDetailsFetcherCastMembersTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<ICastMemberRepository> _castMemberRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IRateLimitPolicyConfig> _rateLimitConfigMock;

        private readonly Mock<IRetryPolicyConfig> _retryConfigMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieDetailsFetcherCastMembers _movieDetailsFetcherCastMembers;

        public MovieDetailsFetcherCastMembersTests()
        {
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);
            this._castMemberRepositoryMock = new Mock<ICastMemberRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.CastMembers)
                .Returns(this._castMemberRepositoryMock.Object);

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

            this._movieDetailsFetcherCastMembers = new MovieDetailsFetcherCastMembers(
                this._unitOfWorkMock.Object,
                this._appSettingsManagerMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public async Task PopulateDetails_WithoutMoviesMissingCastMembers_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRepositoryMock.Setup(m => m.GetMoviesWithoutCastMembers()).Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherCastMembers.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieCastMembersAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PopulateDetails_WithMoviesMissingCastMembers_ShouldCallMovieAPIClient()
        {
            // arrange
            int externalId = 101;
            var movieWithoutCastMembers = new Movie()
            {
                Title = "the fly",
                ReleaseDate = 1986,
                ExternalId = externalId,
                CastMembers = new List<CastMember>()
            };
            this._castMemberRepositoryMock
                .Setup(a => a.GetAll())
                .Returns(new List<CastMember>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutCastMembers())
                .Returns(new Movie[] { movieWithoutCastMembers });
            this._movieAPIClientMock
                .Setup(m => m.GetMovieCastMembersAsync(It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Empty<MovieCastMemberResult>());

            // act
            await this._movieDetailsFetcherCastMembers.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieCastMembersAsync(externalId), Times.Once);
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingCastMembers_WithExistingCastMemberResultInRepo_ShouldBePopulatedWithExistingCastMember()
        {
            // arrange
            var castMemberResult = new MovieCastMemberResult() { Name = "joaquin phoenix", ExternalId = 201 };
            var castMember = (CastMember)castMemberResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutCastMembers = new Movie()
            {
                Title = "i'm still here",
                ReleaseDate = 2010,
                ExternalId = firstExternalId,
                CastMembers = new List<CastMember>()
            };
            var secondMovieWithoutCastMembers = new Movie()
            {
                Title = "joker",
                ReleaseDate = 2019,
                ExternalId = secondExternalId,
                CastMembers = new List<CastMember>()
            };
            this._castMemberRepositoryMock
                .Setup(a => a.GetAll())
                .Returns(new List<CastMember>() { castMember });
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutCastMembers())
                .Returns(new Movie[] { firstMovieWithoutCastMembers, secondMovieWithoutCastMembers });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieCastMembersAsync(It.Is<int>(i => i == firstExternalId | i == secondExternalId)))
                .ReturnsAsync(new MovieCastMemberResult[] { castMemberResult });

            // act
            await this._movieDetailsFetcherCastMembers.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutCastMembers.CastMembers.FirstOrDefault().Should().BeSameAs(castMember);
                secondMovieWithoutCastMembers.CastMembers.FirstOrDefault().Should().BeSameAs(castMember);
            }
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingCastMembers_WithoutSuchCastMembersInRepo_ShouldBePopulatedWithNewCastMembers()
        {
            // arrange
            var firstCastMemberResult = new MovieCastMemberResult() { Name = "joaquin phoenix", ExternalId = 201 };
            var firstCastMember = (CastMember)firstCastMemberResult;

            var secondCastMemberResult = new MovieCastMemberResult() { Name = "adrien brody", ExternalId = 202 };
            var secondCastMember = (CastMember)secondCastMemberResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutCastMembers = new Movie()
            {
                Title = "i'm still here",
                ReleaseDate = 2010,
                ExternalId = firstExternalId,
                CastMembers = new List<CastMember>()
            };
            var secondMovieWithoutCastMembers = new Movie()
            {
                Title = "the village",
                ReleaseDate = 2004,
                ExternalId = secondExternalId,
                CastMembers = new List<CastMember>()
            };

            this._castMemberRepositoryMock
                .Setup(a => a.GetAll())
                .Returns(Enumerable.Empty<CastMember>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutCastMembers())
                .Returns(new Movie[] { firstMovieWithoutCastMembers, secondMovieWithoutCastMembers });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieCastMembersAsync(firstExternalId))
                .ReturnsAsync(new MovieCastMemberResult[] { firstCastMemberResult });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieCastMembersAsync(secondExternalId))
                .ReturnsAsync(new MovieCastMemberResult[] { firstCastMemberResult, secondCastMemberResult });

            // act
            await this._movieDetailsFetcherCastMembers.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutCastMembers.CastMembers.Should().BeEquivalentTo(new List<CastMember>() { firstCastMember });
                secondMovieWithoutCastMembers.CastMembers.Should().BeEquivalentTo(new List<CastMember>() { firstCastMember, secondCastMember });
            }
        }

        [Fact]
        public async Task PopulateDetails_WithMoviesMissingActors_WithoutSuchActorsInRepo_WithSameActorForAllMovies_ShouldBePopulatedWithTheSameActorEntity()
        {
            // arrange
            var firstActorResult = new MovieCastMemberResult() { Name = "joaquin phoenix", ExternalId = 201 };
            var secondActorResult = new MovieCastMemberResult() { Name = "adrien brody", ExternalId = 202 };

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutActors = new Movie()
            {
                Title = "i'm still here",
                ReleaseDate = 2010,
                ExternalId = firstExternalId,
                CastMembers = new List<CastMember>()
            };
            var secondMovieWithoutActors = new Movie()
            {
                Title = "the village",
                ReleaseDate = 2004,
                ExternalId = secondExternalId,
                CastMembers = new List<CastMember>()
            };

            this._castMemberRepositoryMock
                .Setup(a => a.GetAll())
                .Returns(Enumerable.Empty<CastMember>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutCastMembers())
                .Returns(new Movie[] { firstMovieWithoutActors, secondMovieWithoutActors });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieCastMembersAsync(firstExternalId))
                .ReturnsAsync(new MovieCastMemberResult[] { firstActorResult });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieCastMembersAsync(secondExternalId))
                .ReturnsAsync(new MovieCastMemberResult[] { firstActorResult, secondActorResult });

            // act
            await this._movieDetailsFetcherCastMembers.PopulateDetails();

            // assert
            firstMovieWithoutActors.CastMembers
                .First(a => a.ExternalId == firstActorResult.ExternalId)
                .Should()
                .BeSameAs(secondMovieWithoutActors.CastMembers.First(a => a.ExternalId == firstActorResult.ExternalId));
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task PopulateDetails_WithLimitOnNumberOfApiCalls_ShouldNotExceedLimit(int maxApiCalls)
        {
            // arrange
            var firstMovie = new Movie() { Title = "My Cousin Vinny", ReleaseDate = 1992, ExternalId = 101, CastMembers = new List<CastMember>() };
            var secondMovie = new Movie() { Title = "Payback", ReleaseDate = 1999, ExternalId = 102, CastMembers = new List<CastMember>() };
            var thirdMovie = new Movie() { Title = "Office Space", ReleaseDate = 1999, ExternalId = 103, CastMembers = new List<CastMember>() };

            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutCastMembers())
                .Returns(new[] { firstMovie, secondMovie, thirdMovie });

            this._castMemberRepositoryMock.Setup(d => d.GetAll()).Returns(Enumerable.Empty<CastMember>());

            this._movieAPIClientMock
                .Setup(m => m.GetMovieCastMembersAsync(It.IsAny<int>()))
                .ReturnsAsync(new MovieCastMemberResult[] { new MovieCastMemberResult() });

            // act
            await this._movieDetailsFetcherCastMembers.PopulateDetails(maxApiCalls);

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieCastMembersAsync(It.IsAny<int>()), Times.Exactly(maxApiCalls));
        }

    }

}
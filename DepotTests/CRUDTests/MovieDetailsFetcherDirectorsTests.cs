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
    public class MovieDetailsFetcherDirectorsTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IDirectorRepository> _directorRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IRateLimitPolicyConfig> _rateLimitConfigMock;

        private readonly Mock<IRetryPolicyConfig> _retryConfigMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieDetailsFetcherDirectors _movieDetailsFetcherDirectors;

        public MovieDetailsFetcherDirectorsTests()
        {
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);
            this._directorRepositoryMock = new Mock<IDirectorRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Directors)
                .Returns(this._directorRepositoryMock.Object);

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

            this._movieDetailsFetcherDirectors = new MovieDetailsFetcherDirectors(
                this._unitOfWorkMock.Object,
                this._appSettingsManagerMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public async Task PopulateDetails_WithoutMoviesMissingDirectors_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRepositoryMock.Setup(m => m.GetMoviesWithoutDirectors()).Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherDirectors.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieDirectorsAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PopulateDetails_WithMoviesMissingDirectors_ShouldCallMovieAPIClient()
        {
            // arrange
            int externalId = 101;
            var movieWithoutDirectors = new Movie() {
                Title = "the fly", ReleaseDate = 1986, ExternalId = externalId, Directors = new List<Director>()
            };
            this._directorRepositoryMock
                .Setup(d => d.GetAll())
                .Returns(new List<Director>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutDirectors())
                .Returns(new Movie[] { movieWithoutDirectors });
            this._movieAPIClientMock
                .Setup(m => m.GetMovieDirectorsAsync(It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Empty<MovieDirectorResult>());

            // act
            await this._movieDetailsFetcherDirectors.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieDirectorsAsync(externalId), Times.Once);
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingDirectors_WithExistingDirectorResultInRepo_ShouldBePopulatedWithExistingDirector()
        {
            // arrange
            var directorResult = new MovieDirectorResult() { Name = "terrence malick", ExternalId = 201 };
            var director = (Director)directorResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutDirectors = new Movie() {
                Title = "badlands", ReleaseDate = 1973, ExternalId = firstExternalId, Directors = new List<Director>()
            };
            var secondMovieWithoutDirectors = new Movie() {
                Title = "a hidden life", ReleaseDate = 2019, ExternalId = secondExternalId, Directors = new List<Director>()
            };
            this._directorRepositoryMock
                .Setup(d => d.GetAll())
                .Returns(new List<Director>() { director });
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutDirectors())
                .Returns(new Movie[] { firstMovieWithoutDirectors, secondMovieWithoutDirectors });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieDirectorsAsync(It.Is<int>(i => i == firstExternalId | i == secondExternalId)))
                .ReturnsAsync(new MovieDirectorResult[] { directorResult });

            // act
            await this._movieDetailsFetcherDirectors.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutDirectors.Directors.FirstOrDefault().Should().BeSameAs(director);
                secondMovieWithoutDirectors.Directors.FirstOrDefault().Should().BeSameAs(director);
            }
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingDirectors_WithoutSuchDirectorsInRepo_ShouldBePopulatedWithNewDirectors()
        {
            // arrange
            var firstDirectorResult = new MovieDirectorResult() { Name = "terrence malick", ExternalId = 201 };
            var firstDirector = (Director)firstDirectorResult;

            var secondDirectorResult = new MovieDirectorResult() { Name = "paul verhoeven", ExternalId = 202 };
            var secondDirector = (Director)secondDirectorResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            int thirdExternalId = 103;
            var firstMovieWithoutDirectors = new Movie() {
                Title = "badlands", ReleaseDate = 1973, ExternalId = firstExternalId, Directors = new List<Director>()
            };
            var secondMovieWithoutDirectors = new Movie() {
                Title = "total recall", ReleaseDate = 1989, ExternalId = secondExternalId, Directors = new List<Director>()
            };
            var thirdMovieWithoutDirectors = new Movie() {
                Title = "black book", ReleaseDate = 2006, ExternalId = thirdExternalId, Directors = new List<Director>()
            };

            this._directorRepositoryMock
                .Setup(d => d.GetAll())
                .Returns(Enumerable.Empty<Director>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutDirectors())
                .Returns(new Movie[] { firstMovieWithoutDirectors, secondMovieWithoutDirectors, thirdMovieWithoutDirectors });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieDirectorsAsync(firstExternalId))
                .ReturnsAsync(new MovieDirectorResult[] { firstDirectorResult });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieDirectorsAsync(It.Is<int>(i => i == secondExternalId | i == thirdExternalId)))
                .ReturnsAsync(new MovieDirectorResult[] { secondDirectorResult });

            // act
            await this._movieDetailsFetcherDirectors.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutDirectors.Directors.Should().BeEquivalentTo(new List<Director>() { firstDirector });
                secondMovieWithoutDirectors.Directors.Should().BeEquivalentTo(new List<Director>() { secondDirector });
                thirdMovieWithoutDirectors.Directors.Should().BeEquivalentTo(new List<Director>() { secondDirector });
            }
        }


        [Fact]
        public async Task PopulateDetails_WithMoviesMissingDirectors_WithoutSuchDirectorsInRepo_WithSameDirectorForAllMovies_ShouldBePopulatedWithTheSameDirectorEntity()
        {
            // arrange
            var directorResult = new MovieDirectorResult() { Name = "terrence malick", ExternalId = 201 };

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutDirectors = new Movie()
            {
                Title = "badlands",
                ReleaseDate = 1973,
                ExternalId = firstExternalId,
                Directors = new List<Director>()
            };
            var secondMovieWithoutDirectors = new Movie()
            {
                Title = "the thin red line",
                ReleaseDate = 1998,
                ExternalId = secondExternalId,
                Directors = new List<Director>()
            };

            this._directorRepositoryMock
                .Setup(d => d.GetAll())
                .Returns(Enumerable.Empty<Director>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutDirectors())
                .Returns(new Movie[] { firstMovieWithoutDirectors, secondMovieWithoutDirectors });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieDirectorsAsync(firstExternalId))
                .ReturnsAsync(new MovieDirectorResult[] { directorResult });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieDirectorsAsync(secondExternalId))
                .ReturnsAsync(new MovieDirectorResult[] { directorResult });

            // act
            await this._movieDetailsFetcherDirectors.PopulateDetails();

            // assert
            firstMovieWithoutDirectors.Directors
                .First(d => d.ExternalId == directorResult.ExternalId)
                .Should()
                .BeSameAs(secondMovieWithoutDirectors.Directors.First(d => d.ExternalId == directorResult.ExternalId));
        }


        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        public async Task PopulateDetails_WithLimitOnNumberOfApiCalls_ShouldNotExceedLimit(int maxApiCalls)
        {
            // arrange
            var firstMovie = new Movie() { Title = "My Cousin Vinny", ReleaseDate = 1992, ExternalId = 101, Directors = new List<Director>() };
            var secondMovie = new Movie() { Title = "Payback", ReleaseDate = 1999, ExternalId = 102, Directors = new List<Director>() };
            var thirdMovie = new Movie() { Title = "Office Space", ReleaseDate = 1999, ExternalId = 103, Directors = new List<Director>() };

            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutDirectors())
                .Returns(new[] { firstMovie, secondMovie,thirdMovie });

            this._directorRepositoryMock.Setup(d => d.GetAll()).Returns(Enumerable.Empty<Director>());

            this._movieAPIClientMock
                .Setup(m => m.GetMovieDirectorsAsync(It.IsAny<int>()))
                .ReturnsAsync(new MovieDirectorResult[] { new MovieDirectorResult() });

            // act
            await this._movieDetailsFetcherDirectors.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieDirectorsAsync(It.IsAny<int>()), Times.Exactly(maxApiCalls));
        }
    }
}
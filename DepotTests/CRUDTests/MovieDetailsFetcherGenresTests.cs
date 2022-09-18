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
    public class MovieDetailsFetcherGenresTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IGenreRepository> _genreRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IFileSystemIOWrapper> _fileSystemIOWrapperMock;

        private readonly Mock<IRateLimitPolicyConfig> _rateLimitConfigMock;

        private readonly Mock<IRetryPolicyConfig> _retryConfigMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

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

            this._fileSystemIOWrapperMock = new Mock<IFileSystemIOWrapper>();

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

            this._movieDetailsFetcherGenres = new MovieDetailsFetcherGenres(
                this._unitOfWorkMock.Object,
                this._fileSystemIOWrapperMock.Object,
                this._appSettingsManagerMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public async Task PopulateDetails_WithoutMoviesMissingGenres_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRepositoryMock.Setup(m => m.GetMoviesWithoutGenres()).Returns(Enumerable.Empty<Movie>());

            // act
            await this._movieDetailsFetcherGenres.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieGenresAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task PopulateDetails_WithMoviesMissingGenres_ShouldCallMovieAPIClient()
        {
            // arrange
            int externalId = 101;
            var movieWithoutGenres = new Movie() {
                Title = "the fly", ReleaseDate = 1986, ExternalId = externalId , Genres = new List<Genre>()
            };
            this._genreRepositoryMock
                .Setup(g => g.GetAll())
                .Returns(new List<Genre>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutGenres())
                .Returns(new Movie[] { movieWithoutGenres });
            this._movieAPIClientMock
                .Setup(m => m.GetMovieGenresAsync(It.IsAny<int>()))
                .ReturnsAsync(Enumerable.Empty<MovieGenreResult>());

            // act
            await this._movieDetailsFetcherGenres.PopulateDetails();

            // assert
            this._movieAPIClientMock.Verify(cl => cl.GetMovieGenresAsync(externalId), Times.Once);
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingGenres_WithExistingGenreResultInRepo_ShouldBePopulatedWithExistingGenre()
        {
            // arrange
            var dramaGenreResult = new MovieGenreResult() { Name = "drama", ExternalId = 201 };
            var dramaGenre = (Genre)dramaGenreResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutGenres = new Movie() {
                Title = "fish tank", ReleaseDate = 2009, ExternalId = firstExternalId , Genres = new List<Genre>()
            };
            var secondMovieWithoutGenres = new Movie() {
                Title = "gummo", ReleaseDate = 1997, ExternalId = secondExternalId, Genres = new List<Genre>()
            };
            this._genreRepositoryMock
                .Setup(m => m.GetAll())
                .Returns(new List<Genre>() { dramaGenre });
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutGenres())
                .Returns(new Movie[] { firstMovieWithoutGenres, secondMovieWithoutGenres });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieGenresAsync(It.Is<int>(i => i == firstExternalId | i == secondExternalId)))
                .ReturnsAsync(new MovieGenreResult[] { dramaGenreResult });

            // act
            await this._movieDetailsFetcherGenres.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutGenres.Genres.FirstOrDefault().Should().BeSameAs(dramaGenre);
                secondMovieWithoutGenres.Genres.FirstOrDefault().Should().BeSameAs(dramaGenre);
            }
        }

        [Fact]
        public async Task PopulateDetails_WithMovieMissingGenres_WithoutSuchGenresInRepo_ShouldBePopulatedWithNewGenres()
        {
            // arrange
            var dramaGenreResult = new MovieGenreResult() { Name = "drama", ExternalId = 201 };
            var dramaGenre = (Genre)dramaGenreResult;

            var horrorGenreResult = new MovieGenreResult() { Name = "horror", ExternalId = 202 };
            var horrorGenre = (Genre)horrorGenreResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutGenres = new Movie() {
                Title = "the fly", ReleaseDate = 1986, ExternalId = firstExternalId , Genres = new List<Genre>()
            };
            var secondMovieWithoutGenres = new Movie() {
                Title = "gummo", ReleaseDate = 1997, ExternalId = secondExternalId, Genres = new List<Genre>()
            };

            this._genreRepositoryMock
                .Setup(g => g.GetAll())
                .Returns(Enumerable.Empty<Genre>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutGenres())
                .Returns(new Movie[] { firstMovieWithoutGenres, secondMovieWithoutGenres });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieGenresAsync(firstExternalId))
                .ReturnsAsync(new MovieGenreResult[] { horrorGenreResult, dramaGenreResult });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieGenresAsync(secondExternalId))
                .ReturnsAsync(new MovieGenreResult[] { dramaGenreResult });

            // act
            await this._movieDetailsFetcherGenres.PopulateDetails();

            // assert
            using (new AssertionScope())
            {
                firstMovieWithoutGenres.Genres.Should().BeEquivalentTo(new List<Genre>() { horrorGenre, dramaGenre });
                secondMovieWithoutGenres.Genres.Should().BeEquivalentTo(new List<Genre>() { dramaGenre });
            }
        }


        [Fact]
        public async Task PopulateDetails_WithMoviesMissingGenres_WithoutSuchGenresInRepo_WithSameGenreForAllMovies_ShouldBePopulatedWithTheSameGenreEntity()
        {
            var dramaGenreResult = new MovieGenreResult() { Name = "drama", ExternalId = 201 };
            var dramaGenre = (Genre)dramaGenreResult;

            var horrorGenreResult = new MovieGenreResult() { Name = "horror", ExternalId = 202 };
            var horrorGenre = (Genre)horrorGenreResult;

            int firstExternalId = 101;
            int secondExternalId = 102;
            var firstMovieWithoutGenres = new Movie()
            {
                Title = "the fly",
                ReleaseDate = 1986,
                ExternalId = firstExternalId,
                Genres = new List<Genre>()
            };
            var secondMovieWithoutGenres = new Movie()
            {
                Title = "gummo",
                ReleaseDate = 1997,
                ExternalId = secondExternalId,
                Genres = new List<Genre>()
            };

            this._genreRepositoryMock
                .Setup(g => g.GetAll())
                .Returns(Enumerable.Empty<Genre>());
            this._movieRepositoryMock
                .Setup(m => m.GetMoviesWithoutGenres())
                .Returns(new Movie[] { firstMovieWithoutGenres, secondMovieWithoutGenres });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieGenresAsync(firstExternalId))
                .ReturnsAsync(new MovieGenreResult[] { horrorGenreResult, dramaGenreResult });
            this._movieAPIClientMock
                .Setup(cl => cl.GetMovieGenresAsync(secondExternalId))
                .ReturnsAsync(new MovieGenreResult[] { dramaGenreResult });

            // act
            await this._movieDetailsFetcherGenres.PopulateDetails();

            // assert
            // both movies should hold the same exact object for the drama genre
            firstMovieWithoutGenres.Genres
                .First(g => g.ExternalId == dramaGenre.ExternalId)
                .Should()
                .BeSameAs(secondMovieWithoutGenres.Genres.First(g => g.ExternalId == dramaGenre.ExternalId));
        }

    }
}
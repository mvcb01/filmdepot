using Xunit;
using Moq;
using FluentAssertions;

using FilmCRUD;
using FilmCRUD.Interfaces;
using FilmCRUD.CustomExceptions;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using ConfigUtils.Interfaces;
using MovieAPIClients.Interfaces;
using MovieAPIClients;
using System.Linq.Expressions;
using System.Collections.Generic;
using System;
using System.Linq;
using FluentAssertions.Execution;

namespace DepotTests.CRUDTests
{
    public class RipToMovieLinkerTests
    {
        private readonly Mock<IMovieRipRepository> _movieRipRepositoryMock;

        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IFileSystemIOWrapper> _fileSystemIOWrapper;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

        private readonly RipToMovieLinker _ripToMovieLinker;

        public RipToMovieLinkerTests()
        {
            this._movieRipRepositoryMock = new Mock<IMovieRipRepository>();
            this._movieRepositoryMock = new Mock<IMovieRepository>();

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.MovieRips)
                .Returns(this._movieRipRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);

            this._fileSystemIOWrapper = new Mock<IFileSystemIOWrapper>();
            this._movieAPIClientMock = new Mock<IMovieAPIClient>();
            this._appSettingsManagerMock = new Mock<IAppSettingsManager>();

            this._ripToMovieLinker = new RipToMovieLinker(
                this._unitOfWorkMock.Object,
                this._fileSystemIOWrapper.Object,
                this._appSettingsManagerMock.Object,
                this._movieAPIClientMock.Object);
        }

        [Fact]
        public void GetMovieRipsToLink_WithRipFilenamesToIgnore_ShouldNotReturnThem()
        {
            // arrange
            MovieRip[] allRipsToLink = {
                new MovieRip() { FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]", ParsedTitle = "khrustalyov my car"},
                new MovieRip() { FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN", ParsedTitle = "the fly" },
                new MovieRip() { FileName = "Sorcerer.1977.1080p.BluRay.x264-HD4U", ParsedTitle = "sorcerer"}
            };
            this._movieRipRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()))
                .Returns(allRipsToLink);

            string[] ripFilenamesToIgnore = {
                "Sorcerer.1977.1080p.BluRay.x264-HD4U",
                "inexistent.file.480p.x264-DUMMYGROUP"
            };
            this._appSettingsManagerMock.Setup(a => a.GetRipFilenamesToIgnoreOnLinking()).Returns(ripFilenamesToIgnore);

            // act
            IEnumerable<MovieRip> ripsToLinkActual = this._ripToMovieLinker.GetMovieRipsToLink();

            // assert
            MovieRip[] ripsToLinkExpected = {
                new MovieRip() { FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN", ParsedTitle = "the fly" },
                new MovieRip() { FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]", ParsedTitle = "khrustalyov my car"}
            };
            ripsToLinkActual.Should().BeEquivalentTo(ripsToLinkExpected);
        }

        [Fact]
        public void GetMovieRipsToLink_WithManualExternalIds_ShouldNotReturnThem()
        {
            // arrange
            MovieRip[] allRipsToLink = {
                new MovieRip() { FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]", ParsedTitle = "khrustalyov my car"},
                new MovieRip() { FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN", ParsedTitle = "the fly" },
                new MovieRip() { FileName = "Sorcerer.1977.1080p.BluRay.x264-HD4U", ParsedTitle = "sorcerer"}
            };
            this._movieRipRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()))
                .Returns(allRipsToLink);

            var manualExternalIds = new Dictionary<string, int>() {
                { "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]", 100784 },
                { "inexistent.file.480p.x264-DUMMYGROUP", 999}
            };
            this._appSettingsManagerMock
                .Setup(a => a.GetManualExternalIds())
                .Returns(manualExternalIds);

            // act
            IEnumerable<MovieRip> ripsToLinkActual = this._ripToMovieLinker.GetMovieRipsToLink();

            // assert
            MovieRip[] ripsToLinkExpected = {
                new MovieRip() { FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN", ParsedTitle = "the fly" },
                new MovieRip() { FileName = "Sorcerer.1977.1080p.BluRay.x264-HD4U", ParsedTitle = "sorcerer"}
            };
            ripsToLinkActual.Should().BeEquivalentTo(ripsToLinkExpected);

        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WithoutAnyMatchesInRepo_ShouldReturnNull()
        {
            // arrange
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(It.IsAny<string>()))
                .Returns(Enumerable.Empty<Movie>());

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntityInRepo(
                new MovieRip() { ParsedTitle = "some.file.480p.x264-DUMMYGROUP" }
                );

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WhenParsedTitleHasMatchInMovieRepository_ShouldReturnTheMatchedMovie()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                ParsedTitle = "khrustalyov my car"
                };
            var movieMatch = new Movie() { Title = "Khrustalyov, My Car!", ReleaseDate = 1998 };
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(It.IsAny<string>()))
                .Returns(new Movie[] { movieMatch });

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntityInRepo(movieRip);

            //assert
            result.Should().Be(movieMatch);
        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WhenParsedTitleHasMatchInMovieRepository_ShouldCallMovieRepositorySearchMoviesWithTitleMethodOnce()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                ParsedTitle = "khrustalyov my car"
                };

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntityInRepo(movieRip);

            // assert
            this._movieRepositoryMock.Verify(m => m.SearchMoviesWithTitle(movieRip.ParsedTitle), Times.Once);
        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WithoutParsedReleaseDate_WithSeveralMatchesInRepo_ShouldThrowMultipleMovieMatchesError()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN",
                ParsedTitle = "the fly",
                ParsedReleaseDate = null  // só para ser explícito
                };
            Movie[] movieMatches = {
                new Movie() { Title = "The Fly", ReleaseDate = 1958 },
                new Movie() { Title = "The Fly", ReleaseDate = 1986 }
            };
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(movieRip.ParsedTitle))
                .Returns(movieMatches);

            // act
            // nada a fazer...

            // assert
            this._ripToMovieLinker
                .Invoking(r => r.FindRelatedMovieEntityInRepo(movieRip))
                .Should()
                .Throw<MultipleMovieMatchesError>();
        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WithParsedReleaseDate_WithSeveralMatchesInRepo_ShouldThrowMultipleMovieMatchesError()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN",
                ParsedTitle = "the fly",
                ParsedReleaseDate = "1986"
                };
            Movie[] movieMatches = {
                new Movie() { Title = "The Fly", ReleaseDate = 1986 },
                new Movie() { Title = "The Fly", ReleaseDate = 1986 }
            };
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(movieRip.ParsedTitle))
                .Returns(movieMatches);

            // act
            // nada a fazer...

            // assert
            this._ripToMovieLinker
                .Invoking(r => r.FindRelatedMovieEntityInRepo(movieRip))
                .Should()
                .Throw<MultipleMovieMatchesError>();
        }

        [Fact]
        public async void SearchAndLinkAsync_WithoutMatchesInRepo_ShouldCallFindMovieOnlineAsyncMethod()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                ParsedTitle = "khrustalyov my car"
            };
            MovieRip[] ripsToLink = { movieRip };
            this._movieRipRepositoryMock
                .Setup(m => m.Find((It.IsAny<Expression<Func<MovieRip, bool>>>())))
                .Returns(ripsToLink);
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(It.IsAny<string>()))
                .Returns(Enumerable.Empty<Movie>());
            this._appSettingsManagerMock.Setup(a => a.GetWarehouseContentsTextFilesDirectory()).Returns("");

            // act
            await this._ripToMovieLinker.SearchAndLinkAsync();

            // assert
            this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async void SearchAndLinkAsync_WithoutMatchesInRepo_WithOnlineMatch_ShouldLinkRipToOnlineMatch()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                ParsedTitle = "khrustalyov my car"
            };
            var searchResult = new MovieSearchResult() { ExternalId = 1, Title = "Khrustalyov, My Car!", ReleaseDate = 1998 };
            this._movieRipRepositoryMock
                .Setup(m => m.Find((It.IsAny<Expression<Func<MovieRip, bool>>>())))
                .Returns(new MovieRip[] { movieRip });
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(It.IsAny<string>()))
                .Returns(Enumerable.Empty<Movie>());
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("khrustalyov"))))
                .ReturnsAsync(new MovieSearchResult[] { searchResult } );
            this._appSettingsManagerMock.Setup(a => a.GetWarehouseContentsTextFilesDirectory()).Returns("");

            // act
            await this._ripToMovieLinker.SearchAndLinkAsync();

            // assert
            movieRip.Movie.Should().BeEquivalentTo(new { Title = "Khrustalyov, My Car!", ReleaseDate = 1998, ExternalId = 1 });
        }

        [Fact]
        public async void LinkFromManualExternalIdsAsync_WithoutManualExternalIds_ShouldNotCallApiClient()
        {
            // arrange
            this._movieRipRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()))
                .Returns(Enumerable.Empty<MovieRip>());
            this._appSettingsManagerMock
                .Setup(a => a.GetManualExternalIds())
                .Returns(new Dictionary<string, int>());

            // act
            await this._ripToMovieLinker.LinkFromManualExternalIdsAsync();

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieInfoAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async void LinkFromManualExternalIdsAsync_WithManualExternalIds_ShouldLinkUnlinkedMovieRipsCorrectly()
        {
            // arrange
            int firstExternalId = 101;
            var firstMovieRipToLink = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]"
            };
            int secondExternalId = 102;
            var secondMovieRipToLink = new MovieRip() {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN"
            };
            var manualExternalIds = new Dictionary<string, int>() {
                { "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]", firstExternalId },
                { "The.Fly.1986.1080p.BluRay.x264-TFiN", secondExternalId }
            };
            (string Title, string OriginalTitle, int ReleaseDate) firstMovieInfo = ("Khrustalyov, My Car!", "Хрусталёв, машину!", 1998);
            (string Title, string OriginalTitle, int ReleaseDate) secondMovieInfo = ("The Fly", "The Fly", 1986);
            this._movieRipRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()))
                .Returns(new MovieRip[] { firstMovieRipToLink, secondMovieRipToLink });
            this._movieAPIClientMock
                .Setup(m => m.GetMovieInfoAsync(firstExternalId))
                .ReturnsAsync(firstMovieInfo);
            this._movieAPIClientMock
                .Setup(m => m.GetMovieInfoAsync(secondExternalId))
                .ReturnsAsync(secondMovieInfo);
            this._appSettingsManagerMock
                .Setup(a => a.GetManualExternalIds())
                .Returns(manualExternalIds);
            this._movieRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(Enumerable.Empty<Movie>());

            // act
            await this._ripToMovieLinker.LinkFromManualExternalIdsAsync();

            // assert
            using (new AssertionScope())
            {
                firstMovieRipToLink.Movie
                    .Should()
                    .BeEquivalentTo(new {
                        Title = firstMovieInfo.Title,
                        OriginalTitle = firstMovieInfo.OriginalTitle,
                        ReleaseDate = firstMovieInfo.ReleaseDate,
                        ExternalId = firstExternalId });

                secondMovieRipToLink.Movie
                    .Should()
                    .BeEquivalentTo(new {
                        Title = secondMovieInfo.Title,
                        OriginalTitle = secondMovieInfo.OriginalTitle,
                        ReleaseDate = secondMovieInfo.ReleaseDate,
                        ExternalId = secondExternalId });
            }

        }

        [Fact]
        public async void LinkFromManualExternalIdsAsync_WithManualExternalIds_ShouldIgnoreMatchingManualExternalIdsInLinkedMovieRips()
        {
            // arrange
            int existingExternalId = 101;
            var movieRipToIgnore = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                Movie = new Movie() {
                    Title = "Khrustalyov, My Car!",
                    ExternalId = existingExternalId
                }
            };
            var manualExternalIds = new Dictionary<string, int>() {
                { "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]", existingExternalId }
            };
            this._movieRipRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()))
                .Returns(new MovieRip[] { movieRipToIgnore });
            this._appSettingsManagerMock
                .Setup(a => a.GetManualExternalIds())
                .Returns(manualExternalIds);
            this._movieRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(Enumerable.Empty<Movie>());

            // act
            await this._ripToMovieLinker.LinkFromManualExternalIdsAsync();

            // assert
            this._movieAPIClientMock.Verify(m => m.GetMovieInfoAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async void LinkFromManualExternalIdsAsync_WithManualExternalIds_ShouldOverwriteMismatchingManualExternalIdsInLinkedMovieRips()
        {
            // arrange
            int existingExternalId = 102;
            int newExternalId = 103;
            var movieRipToLinkManually = new MovieRip() {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN",
                // not the correct pone
                Movie = new Movie() {
                    Title = "Return of the Fly",
                    ExternalId = existingExternalId
                }
            };
            var manualExternalIds = new Dictionary<string, int>() {
                { "The.Fly.1986.1080p.BluRay.x264-TFiN", newExternalId }
            };
            (string Title, string OriginalTitle, int ReleaseDate) correctMovieInfo = ("The Fly", "The Fly", 1986);
            this._movieRipRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()))
                .Returns(new MovieRip[] { movieRipToLinkManually });
            this._appSettingsManagerMock
                .Setup(a => a.GetManualExternalIds())
                .Returns(manualExternalIds);
            this._movieAPIClientMock
                .Setup(m => m.GetMovieInfoAsync(newExternalId))
                .ReturnsAsync(correctMovieInfo);
            this._movieRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(Enumerable.Empty<Movie>());

            // act
            await this._ripToMovieLinker.LinkFromManualExternalIdsAsync();

            // assert
            movieRipToLinkManually.Movie
                    .Should()
                    .BeEquivalentTo(new {
                        Title = correctMovieInfo.Title,
                        OriginalTitle = correctMovieInfo.OriginalTitle,
                        ReleaseDate = correctMovieInfo.ReleaseDate,
                        ExternalId = newExternalId });
        }

        [Fact]
        public async void LinkFromManualExternalIdsAsync_WithManualExternalIds_WithExistingMatchInMovieRepo_ShouldUseExistingMatchToLink()
        {
            // arrange
            int externalIdInRepo = 104;
            var movieRipToLinkManually = new MovieRip() {
                FileName = "Sorcerer.1977.1080p.BluRay.x264-HD4U"
            };
            var movieInRepo = new Movie() {
                Title = "Sorcerer",
                OriginalTitle = "Sorcerer",
                ReleaseDate = 1977,
                ExternalId = externalIdInRepo
            };
            var manualExternalIds = new Dictionary<string, int>() {
                { "Sorcerer.1977.1080p.BluRay.x264-HD4U", externalIdInRepo }
            };
            this._movieRipRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()))
                .Returns(new MovieRip[] { movieRipToLinkManually });
            this._movieRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(new Movie[] { movieInRepo });
            this._appSettingsManagerMock
                .Setup(a => a.GetManualExternalIds())
                .Returns(manualExternalIds);

            // act
            await this._ripToMovieLinker.LinkFromManualExternalIdsAsync();

            // assert
            // points to the same object in memory
            movieRipToLinkManually.Movie.Should().BeSameAs(movieInRepo);
        }

    }
}
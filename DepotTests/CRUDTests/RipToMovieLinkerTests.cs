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
using System.Linq.Expressions;
using System.Collections.Generic;
using System;

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
        public void GetMovieRipsToLink_WithRipFilenamesToIgnore_ShouldReturnTheCorrectMovieRips()
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
            IEnumerable<MovieRip> ripsToLinkResult = this._ripToMovieLinker.GetMovieRipsToLink();

            // assert
             MovieRip[] expectedRipsToLink = {
                new MovieRip() { FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN", ParsedTitle = "the fly" },
                new MovieRip() { FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]", ParsedTitle = "khrustalyov my car"}
             };
            ripsToLinkResult.Should().BeEquivalentTo(expectedRipsToLink);
        }

        [Fact]
        public void FindRelatedMovieEntity_WhenParsedTitleHasMatchInMovieRepository_ShouldReturnTheMatchedMovie()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                ParsedTitle = "khrustalyov my car"
                };
            var movieMatch = new Movie() { Title = "Khrustalyov, My Car!", ReleaseDate = 1998 };
            this._movieRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(new Movie[] { movieMatch });

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntity(movieRip);

            //assert
            result.Should().Be(movieMatch);
        }

        [Fact]
        public void FindRelatedMovieEntity_WhenParsedTitleHasMatchInMovieRepository_ShouldNotCallSearchMovieAsync()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                ParsedTitle = "khrustalyov my car"
                };
            var movieMatch = new Movie() { Title = "Khrustalyov, My Car!", ReleaseDate = 1998 };
            this._movieRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(new Movie[] { movieMatch });

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntity(movieRip);

            //assert
            // se já há um match no repo então não deve ser chamado o método SearchMovieAsync do IMovieAPIClient
            this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void FindRelatedMovieEntity_WhenParsedTitleHasMatchInMovieRepository_ShouldCallMovieRepositoryFindMethodOnce()
        {
            // arrange
            var movieRip = new MovieRip() {
                FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                ParsedTitle = "khrustalyov my car"
                };
            var movieMatch = new Movie() { Title = "Khrustalyov, My Car!", ReleaseDate = 1998 };
            this._movieRepositoryMock
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(new Movie[] { movieMatch });

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntity(movieRip);

            // assert
            this._movieRepositoryMock.Verify(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()), Times.Once);
        }

        [Fact]
        public void FindRelatedMovieEntity_WithoutParsedReleaseDate_WithSeveralMatchesInRepo_ShouldThrowMultipleMovieMatchesError()
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
                .Setup(m => m.Find(It.IsAny<Expression<Func<Movie, bool>>>()))
                .Returns(movieMatches);

            // act
            // nada a fazer...

            // assert
            this._ripToMovieLinker
                .Invoking(r => r.FindRelatedMovieEntity(movieRip))
                .Should()
                .Throw<MultipleMovieMatchesError>();
        }

    }
}
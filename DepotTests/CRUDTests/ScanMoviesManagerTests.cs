using Xunit;
using FluentAssertions;
using Moq;
using System;

using FilmDomain.Entities;
using FilmDomain.Interfaces;

namespace DepotTests.CRUDTests
{
    public class ScanMoviesManagerTests
    {
        private readonly Mock<IActorRepository> _actoRepositoryMock;

        private readonly Mock<IGenreRepository> _genreRepositoryMock;

        private readonly Mock<IDirectorRepository> _directorRepositoryMock;

        private readonly Mock<IMovieRepository> _movieRepositoryMock;


        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        public ScanMoviesManagerTests()
        {
            this._actoRepositoryMock = new Mock<IActorRepository>(MockBehavior.Strict);
            this._genreRepositoryMock = new Mock<IGenreRepository>(MockBehavior.Strict);
            this._directorRepositoryMock = new Mock<IDirectorRepository>(MockBehavior.Strict);
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.Actors)
                .Returns(this._actoRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Genres)
                .Returns(this._genreRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Directors)
                .Returns(this._directorRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);
        }

        [Fact]
        public void GetMoviesWithGenres_WithProvidedGenres_ShouldReturnCorrectMovies()
        {
            var dramaGenre = new Genre() { Name = "drama" };
            var horrorGenre = new Genre() { Name = "horror" };

            var firstMovie = new Movie() {
                Title = "the fly", ReleaseDate = 1986, Genres = new Genre[] { dramaGenre, horrorGenre }
            };
            var second = new Movie() {
                Title = "gummo", ReleaseDate = 1997, Genres = new Genre[] { dramaGenre }
            };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            // this._unitOfWorkMock.Setup(u => u.Movies.G)

            // When

            // Then
        }
    }
}
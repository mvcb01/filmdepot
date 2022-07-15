using Xunit;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;

using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmCRUD;

namespace DepotTests.CRUDTests
{
    public class ScanMoviesManagerTests
    {
        private readonly Mock<IActorRepository> _actoRepositoryMock;

        private readonly Mock<IGenreRepository> _genreRepositoryMock;

        private readonly Mock<IDirectorRepository> _directorRepositoryMock;

        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly ScanMoviesManager _scanMoviesManager;

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

            this._scanMoviesManager = new ScanMoviesManager(this._unitOfWorkMock.Object);
        }

        [Fact]
        public void GetMoviesWithGenres_WithProvidedGenres_ShouldReturnCorrectMovies()
        {
            // arrange
            var dramaGenre = new Genre() { Name = "drama" };
            var horrorGenre = new Genre() { Name = "horror" };
            var comedyGenre = new Genre() { Name = "comedy" };

            var firstMovie = new Movie() {
                Title = "the fly", ReleaseDate = 1986, Genres = new Genre[] { dramaGenre, horrorGenre }
            };
            var secondMovie = new Movie() {
                Title = "gummo", ReleaseDate = 1997, Genres = new Genre[] { dramaGenre }
            };
            var thirdMovie = new Movie() {
                Title = "dumb and dumber", ReleaseDate = 1994, Genres = new Genre[] { comedyGenre }
            };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._unitOfWorkMock
                .Setup(u => u.Movies.GetAllMoviesInVisit(visit))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithGenres(visit, dramaGenre, horrorGenre);

            // assert
            actual.Should().BeEquivalentTo(new Movie[] { firstMovie, secondMovie });
        }

        [Fact]
        public void GetMoviesWithActors_WithProvidedActors_ShouldReturnCorrectMovies()
        {
            // arrange
            var firstActor = new Actor() { Name = "jeff goldblum" };
            var secondActor = new Actor() { Name = "bill pullman" };
            var thirdActor = new Actor() { Name = "jim carrey" };

            var firstMovie = new Movie() {
                Title = "the fly", ReleaseDate = 1986, Actors = new Actor[] { firstActor }
            };
            var secondMovie = new Movie() {
                Title = "independence day", ReleaseDate = 1996, Actors = new Actor[] { firstActor, secondActor }
            };
            var thirdMovie = new Movie() {
                Title = "dumb and dumber", ReleaseDate = 1994, Actors = new Actor[] { thirdActor }
            };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._unitOfWorkMock
                .Setup(u => u.Movies.GetAllMoviesInVisit(visit))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithActors(visit, firstActor, secondActor);

            // assert
            actual.Should().BeEquivalentTo(new Movie[] { firstMovie, secondMovie });
        }

        [Fact]
        public void GetMoviesWithDirectors_WithProvidedActors_ShouldReturnCorrectMovies()
        {
            // arrange
            var firstDirector = new Director() { Name = "benny safdie" };
            var secondDirector = new Director() { Name = "josh safdie" };
            var thirdDirector = new Director() { Name = "paul thomas anderson" };

            var firstMovie = new Movie() {
                Title = "uncut gems", ReleaseDate = 2019, Directors = new Director[] { firstDirector, secondDirector }
            };
            var secondMovie = new Movie() {
                Title = "there will be blood", ReleaseDate = 2007, Directors = new Director[] { thirdDirector }
            };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._unitOfWorkMock
                .Setup(u => u.Movies.GetAllMoviesInVisit(visit))
                .Returns(new Movie[] { firstMovie, secondMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithDirectors(visit, firstDirector, secondDirector);

            // assert
            actual.Should().BeEquivalentTo(new Movie[] { firstMovie });

        }

        [Fact]
        public void GetCountByGenre_ShouldReturnCorrectCount()
        {
            // arrange
            var firstMovie = new Movie() { Title = "the fly", ReleaseDate = 1986 };
            var secondMovie = new Movie() {Title = "gummo", ReleaseDate = 1997 };
            var thirdMovie = new Movie() { Title = "dumb and dumber", ReleaseDate = 1994 };

            var dramaGenre = new Genre() { Name = "drama", Movies = new List<Movie>() { firstMovie, secondMovie }};
            var horrorGenre = new Genre() { Name = "horror", Movies = new List<Movie>() { firstMovie } };
            var comedyGenre = new Genre() { Name = "comedy", Movies = new List<Movie>() { thirdMovie } };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._genreRepositoryMock
                .Setup(g => g.GetAll())
                .Returns(new Genre[] { dramaGenre, horrorGenre, comedyGenre });

            // act
            IEnumerable<KeyValuePair<Genre, int>> actual = this._scanMoviesManager.GetCountByGenre(visit);

            // assert
            var expected = new List<KeyValuePair<Genre, int>>() {
                new KeyValuePair<Genre, int>(dramaGenre, 2),
                new KeyValuePair<Genre, int>(horrorGenre, 1),
                new KeyValuePair<Genre, int>(comedyGenre, 1),
            };
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetCountByDirector_ShouldReturnCorrectCount()
        {
            // arrange
            var firstMovie = new Movie() { Title = "uncut gems", ReleaseDate = 2019 };
            var secondMovie = new Movie() { Title = "there will be blood", ReleaseDate = 2007 };
            var thirdMovie = new Movie() { Title = "Licorice Pizza", ReleaseDate = 2021 };

            var firstDirector = new Director() { Name = "benny safdie", Movies = new List<Movie>() { firstMovie } };
            var secondDirector = new Director() { Name = "josh safdie", Movies = new List<Movie>() { firstMovie } };
            var thirdDirector = new Director() { Name = "paul thomas anderson", Movies = new List<Movie>() { secondMovie, thirdMovie } };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._directorRepositoryMock
                .Setup(d => d.GetAll())
                .Returns(new Director[] { firstDirector, secondDirector, thirdDirector });

            // act
            IEnumerable<KeyValuePair<Director, int>> actual = this._scanMoviesManager.GetCountByDirector(visit);

            // assert
            var expected = new List<KeyValuePair<Director, int>>() {
                new KeyValuePair<Director, int>(thirdDirector, 2),
                new KeyValuePair<Director, int>(secondDirector, 1),
                new KeyValuePair<Director, int>(firstDirector, 2)
            };
            actual.Should().BeEquivalentTo(expected);
        }
    }
}

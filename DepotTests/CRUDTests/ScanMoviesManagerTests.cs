using Xunit;
using FluentAssertions;
using Moq;
using System;
using System.Linq;
using System.Collections.Generic;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmCRUD;
using FluentAssertions.Execution;

namespace DepotTests.CRUDTests
{
    public class ScanMoviesManagerTests
    {
        private readonly Mock<IMovieWarehouseVisitRepository> _movieWarehouseVisitRepositoryMock;

        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly ScanMoviesManager _scanMoviesManager;

        public ScanMoviesManagerTests()
        {
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);
            this._movieWarehouseVisitRepositoryMock = new Mock<IMovieWarehouseVisitRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.MovieWarehouseVisits)
                .Returns(this._movieWarehouseVisitRepositoryMock.Object);
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
                .Setup(u => u.Movies.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithGenres(visit, dramaGenre, horrorGenre);

            // assert
            actual.Should().BeEquivalentTo(new Movie[] { firstMovie, secondMovie });
        }

        [Fact]
        public void GetMoviesWithCastMembers_WithProvidedCastMembers_ShouldReturnCorrectMovies()
        {
            // arrange
            var firstCastMember = new CastMember() { Name = "jeff goldblum" };
            var secondCastMember = new CastMember() { Name = "bill pullman" };
            var thirdCastMember = new CastMember() { Name = "jim carrey" };

            var firstMovie = new Movie() {
                Title = "the fly", ReleaseDate = 1986, CastMembers = new CastMember[] { firstCastMember }
            };
            var secondMovie = new Movie() {
                Title = "independence day", ReleaseDate = 1996, CastMembers = new CastMember[] { firstCastMember, secondCastMember }
            };
            var thirdMovie = new Movie() {
                Title = "dumb and dumber", ReleaseDate = 1994, CastMembers = new CastMember[] { thirdCastMember }
            };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._unitOfWorkMock
                .Setup(u => u.Movies.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithCastMembers(visit, firstCastMember, secondCastMember);

            // assert
            actual.Should().BeEquivalentTo(new Movie[] { firstMovie, secondMovie });
        }

        [Fact]
        public void GetMoviesWithDirectors_WithProvidedDirectors_ShouldReturnCorrectMovies()
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
                .Setup(u => u.Movies.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithDirectors(visit, firstDirector, secondDirector);

            // assert
            actual.Should().BeEquivalentTo(new Movie[] { firstMovie });
        }

        [Fact]
        public void GetMoviesWithReleaseDates_WithProvidedDates_ShouldReturnCorrectMovies()
        {
            // arrange
            var firstMovie = new Movie() { Title = "the fly", ReleaseDate = 1986 };
            var secondMovie = new Movie() {Title = "gummo", ReleaseDate = 1997 };
            var thirdMovie = new Movie() { Title = "dumb and dumber", ReleaseDate = 1994 };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithReleaseDates(visit, new int[] { 1994, 1995, 1996, 1997 });

            // assert
            var expected = new Movie[] { secondMovie, thirdMovie };
            actual.Should().BeEquivalentTo(expected);
        }


        [Fact]
        public void GetMoviesWithKeywords_WithProvidedKeywords_ShouldReturnCorrectMovies()
        {
            // arrange
            var firstMovie = new Movie() { Title = "wake in fright", ReleaseDate = 1971, Keywords = new[] { "beer", "australia" } };
            var secondMovie = new Movie() { Title = "animal kingdom", ReleaseDate = 2010, Keywords = new[] { "drug dealer", "australia", "trial" } };
            var thirdMovie = new Movie() { Title = "dumb and dumber", ReleaseDate = 1994, Keywords = new[] { "road trip", "limousine" } };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.GetMoviesWithKeywords(visit, "  AUSTraLia", "bEEr  ");

            // assert
            var expected = new Movie[] { firstMovie, secondMovie };
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetMoviesWithKeywords_WithNullKeywordProperties_ShouldReturnEmptyEnumerable()
        {
            // arrange

            // act

            // assert
        }

        [Fact]
        public void GetCountByGenre_ShouldReturnCorrectCount()
        {
            // arrange
            var dramaGenre = new Genre() { Name = "drama" };
            var horrorGenre = new Genre() { Name = "horror" };
            var comedyGenre = new Genre() { Name = "comedy" };

            var firstMovie = new Movie() { Title = "the fly", ReleaseDate = 1986, Genres = new Genre[] { dramaGenre, horrorGenre } };
            var secondMovie = new Movie() {Title = "wake in fright", ReleaseDate = 1971, Genres = new Genre[] { dramaGenre } };
            var thirdMovie = new Movie() { Title = "dumb and dumber", ReleaseDate = 1994, Genres = new Genre[] { comedyGenre } };
            var fourthMovie = new Movie() { Title = "begotten", ReleaseDate = 1989, Genres = Array.Empty<Genre>() };
            var fifthMovie = new Movie() { Title = "gummo", ReleaseDate = 1997, Genres = Array.Empty<Genre>() };

            var moviesInVisit = new Movie[] { firstMovie, secondMovie, thirdMovie, fourthMovie, fifthMovie };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(moviesInVisit);

            // act
            IEnumerable<KeyValuePair<Genre, int>> actual = this._scanMoviesManager.GetCountByGenre(visit, out int withoutGenres);

            // assert
            var expected = new List<KeyValuePair<Genre, int>>() {
                new KeyValuePair<Genre, int>(dramaGenre, 2),
                new KeyValuePair<Genre, int>(horrorGenre, 1),
                new KeyValuePair<Genre, int>(comedyGenre, 1),
            };

            using (new AssertionScope())
            {
                actual.Should().BeEquivalentTo(expected);
                withoutGenres.Should().Be(moviesInVisit.Where(m => !m.Genres.Any()).Count());
            }
        }

        [Fact]
        public void GetCountByCastMember_ShouldReturnCorrectCount()
        {
            // arrange
            var firstCastMember = new CastMember() { Name = "jeff goldblum" };
            var secondCastMember = new CastMember() { Name = "bill pullman" };
            var thirdCastMember = new CastMember() { Name = "jim carrey" };

            var firstMovie = new Movie() { Title = "the fly", ReleaseDate = 1986, CastMembers = new CastMember[] { firstCastMember, secondCastMember } };
            var secondMovie = new Movie() { Title = "independence day", ReleaseDate = 1996, CastMembers = new CastMember[] { firstCastMember } };
            var thirdMovie = new Movie() { Title = "dumb and dumber", ReleaseDate = 1994, CastMembers = new CastMember[] { thirdCastMember } };
            var fourthMovie = new Movie() { Title = "begotten", ReleaseDate = 1989, CastMembers = Array.Empty<CastMember>() };
            var fifthMovie = new Movie() { Title = "gummo", ReleaseDate = 1997, CastMembers = Array.Empty<CastMember>() };

            var moviesInVisit = new Movie[] { firstMovie, secondMovie, thirdMovie, fourthMovie, fifthMovie };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(moviesInVisit);

            // act
            IEnumerable<KeyValuePair<CastMember, int>> actual = this._scanMoviesManager.GetCountByCastMember(visit, out int withoutCastMembers);

            // assert
            var expected = new List<KeyValuePair<CastMember, int>>() {
                new KeyValuePair<CastMember, int>(firstCastMember, 2),
                new KeyValuePair<CastMember, int>(secondCastMember, 1),
                new KeyValuePair<CastMember, int>(thirdCastMember, 1)
            };

            using (new AssertionScope())
            {
                actual.Should().BeEquivalentTo(expected);
                withoutCastMembers.Should().Be(moviesInVisit.Where(m => !m.CastMembers.Any()).Count());
            }
        }

        [Fact]
        public void GetCountByDirector_ShouldReturnCorrectCount()
        {
            // arrange
            var firstDirector = new Director() { Name = "benny safdie" };
            var secondDirector = new Director() { Name = "josh safdie" };
            var thirdDirector = new Director() { Name = "paul thomas anderson" };

            var firstMovie = new Movie() { Title = "uncut gems", ReleaseDate = 2019, Directors = new Director[] { firstDirector, secondDirector } };
            var secondMovie = new Movie() { Title = "there will be blood", ReleaseDate = 2007, Directors = new Director[] { thirdDirector } };
            var thirdMovie = new Movie() { Title = "Licorice Pizza", ReleaseDate = 2021, Directors = new Director[] { thirdDirector } };
            var fourthMovie = new Movie() { Title = "begotten", ReleaseDate = 1989, Directors = Array.Empty<Director>() };
            var fifthMovie = new Movie() { Title = "gummo", ReleaseDate = 1997, Directors = Array.Empty<Director>() };

            var moviesInVisit = new Movie[] { firstMovie, secondMovie, thirdMovie, fourthMovie, fifthMovie };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(moviesInVisit);

            // act
            IEnumerable<KeyValuePair<Director, int>> actual = this._scanMoviesManager.GetCountByDirector(visit, out int withoutDirectors);

            // assert
            var expected = new List<KeyValuePair<Director, int>>() {
                new KeyValuePair<Director, int>(thirdDirector, 2),
                new KeyValuePair<Director, int>(secondDirector, 1),
                new KeyValuePair<Director, int>(firstDirector, 1)
            };

            using (new AssertionScope())
            {
                actual.Should().BeEquivalentTo(expected);
                withoutDirectors.Should().Be(moviesInVisit.Where(m => !m.Directors.Any()).Count());
            }
        }

        [Theory]
        [InlineData("Licorice Pizza")]
        [InlineData("Licorice Pizza 2021")]
        [InlineData("Licorice Pizza (2021)")]
        [InlineData("licorice pizza")]
        [InlineData("licorice pizza 2021")]
        [InlineData("licorice pizza (2021)")]
        [InlineData(" licorice   piZZa")]
        [InlineData(" licorice ! piZZa 2021 -->")]
        [InlineData("??? licorice ==> piZZa (2021)%%$$##")]
        public void SearchMovieEntities_WithTitleMatch_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { Title = "uncut gems", ReleaseDate = 2019 };
            var secondMovie = new Movie() { Title = "there will be blood", ReleaseDate = 2007 };
            var thirdMovie = new Movie() { Title = "Licorice Pizza", ReleaseDate = 2021 };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.SearchMovieEntities(visit, title);

            // assert
            var expected = new Movie[] { thirdMovie };
            actual.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [InlineData("O Que Arde")]
        [InlineData("O Que Arde 2019")]
        [InlineData("O Que Arde (2019)")]
        [InlineData("o que arde")]
        [InlineData("o que arde 2019")]
        [InlineData("o que arde (2019)")]
        [InlineData("  o   qUe arDE  ")]
        [InlineData(" O ! QuE   #( arDe 2019 -->")]
        [InlineData("!!== o // que aRDE (2019)€€**««»")]
        public void SearchMovieEntities_WithOriginalTitleMatch_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { Title = "there will be blood", OriginalTitle = "there will be blood",  ReleaseDate = 2007 };
            var secondMovie = new Movie() { Title = "Licorice Pizza", ReleaseDate = 2021 };
            var thirdMovie = new Movie() { Title = "Fire Will Come", OriginalTitle = "O que Arde", ReleaseDate = 2019 };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.SearchMovieEntities(visit, title);

            // assert
            var expected = new Movie[] { thirdMovie };
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void SearchMovieEntities_WithBothTitleAndOriginalTitleMatch_ShouldNotReturnDuplicates()
        {
            // arrange
            var firstMovie = new Movie() { Title = "there will be blood", ReleaseDate = 2007 };
            var secondMovie = new Movie() { Title = "Licorice Pizza", OriginalTitle = "Licorice Pizza", ReleaseDate = 2021 };
            var thirdMovie = new Movie() { Title = "Fire Will Come", OriginalTitle = "O que Arde", ReleaseDate = 2019 };

            var visit = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(new Movie[] { firstMovie, secondMovie, thirdMovie });

            // act
            IEnumerable<Movie> actual = this._scanMoviesManager.SearchMovieEntities(visit, "«»}}} licorice ==> piZZa (2021)*+-^```");

            // assert
            var expected = new Movie[] { secondMovie };
            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetVisitDiff_WithTwoNullVisits_ShouldThrowArgumentNullException()
        {
            // arrange
            // nothing to do...

            // act
            // nothing to do...

            // assert
            this._scanMoviesManager.Invoking(s => s.GetVisitDiff(null, null)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetVisitDiff_WithNullVisitRight_ShouldThrowArgumentNullException()
        {
            // arrange
            var visitLeft = new MovieWarehouseVisit();

            // act
            // nothing to do...

            // assert
            this._scanMoviesManager.Invoking(s => s.GetVisitDiff(visitLeft, null)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetVisitDiff_WithVisitLeftMoreRecentThanVisitRigh_ShouldThrowArgumentException()
        {
            // arrange
            var visitLeft = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220102", "yyyyMMdd", null) };
            var visitRight = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            // act
            // nothing to to...

            //assert
            this._scanMoviesManager
                .Invoking(r => r.GetVisitDiff(visitLeft, visitRight))
                .Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        public void GetVisitDiff_WithOnlyVisitRight_ShouldReturnEmptyEnumerableInRemovedKeyAndAllRipsInAddedKey()
        {
            // arrange
            var firstRip = new MovieRip() {
                FileName = "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                Movie = new Movie() { Title = "Face Off", ReleaseDate = 1997 }
            };
            var secondRip = new MovieRip() {
                FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                Movie = new Movie() { Title = "Gummo", ReleaseDate = 1997 }
            };
            var thirdRip = new MovieRip() {
                FileName = "Papillon.1973.1080p.BluRay.X264-AMIABLE",
                Movie = new Movie() { Title = "Papillon", ReleaseDate = 1973 }
            };
            var visitRight = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { firstRip, secondRip, thirdRip },
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
                };
            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visitRight.VisitDateTime)))
                .Returns(visitRight.MovieRips.Select(mr => mr.Movie));

            // act
            Dictionary<string, IEnumerable<string>> visitDiff = this._scanMoviesManager.GetVisitDiff(null, visitRight);

            // assert
            using (new AssertionScope())
            {
                visitDiff["removed"].Should().BeEmpty();
                visitDiff["added"].Should().BeEquivalentTo(new string[] {
                    firstRip.Movie.ToString(),
                    secondRip.Movie.ToString(),
                    thirdRip.Movie.ToString() });
            }
        }

        [Fact]
        public void GetVisitDiff_WithTwoVisits_ShouldReturnCorrectDifference()
        {
            // arrange
            var movieWithTwoRips = new Movie() { Title = "Wake In Fright", ReleaseDate = 1971 };

            var firstRip = new MovieRip() {
                FileName = "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                Movie = new Movie() { Title = "Face Off", ReleaseDate = 1997 }
            };
            var secondRip = new MovieRip() {
                FileName = "Wake.In.Fright.1971.1080p.BluRay.H264.AAC-RARBG",
                Movie = movieWithTwoRips
            };
            var thirdRip = new MovieRip() {
                FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                Movie = new Movie() { Title = "Gummo", ReleaseDate = 1997 }
            };
            var fourthRip = new MovieRip() {
                FileName = "Wake.In.Fright.1971.1080p.BluRay.x264.DD2.0-FGT",
                Movie = movieWithTwoRips
            };

            var visitLeft = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { firstRip, secondRip },
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
            };
            var visitRight = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { thirdRip, fourthRip },
                VisitDateTime = DateTime.ParseExact("20220102", "yyyyMMdd", null)
            };

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visitLeft.VisitDateTime)))
                .Returns(visitLeft.MovieRips.Select(mr => mr.Movie));
            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visitRight.VisitDateTime)))
                .Returns(visitRight.MovieRips.Select(mr => mr.Movie));

            // act
            Dictionary<string, IEnumerable<string>> visitDiff = this._scanMoviesManager.GetVisitDiff(visitLeft, visitRight);

            // assert
            IEnumerable<string> removedExpected = new string[] { firstRip.Movie.ToString() };
            IEnumerable<string> addedExpected = new string[] { thirdRip.Movie.ToString() };
            using (new AssertionScope())
            {
                visitDiff["removed"].Should().BeEquivalentTo(removedExpected);
                visitDiff["added"].Should().BeEquivalentTo(addedExpected);
            }
        }

        [Fact]
        public void GetLastVisitDiff_ShouldReturnCorrectDifference()
        {
            // arrange
            var movieWithTwoRips = new Movie() { Title = "Wake In Fright", ReleaseDate = 1971 };
            var movieRip0 = new MovieRip() {
                FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                Movie = new Movie() { Title = "Gummo", ReleaseDate = 1997 }
            };
            var movieRip1 = new MovieRip() {
                FileName = "Papillon.1973.1080p.BluRay.X264-AMIABLE",
                Movie = new Movie() { Title = "Papillon", ReleaseDate = 1973 }
            };
            var movieRip2 = new MovieRip() {
                FileName = "Wake.In.Fright.1971.1080p.BluRay.H264.AAC-RARBG",
                Movie = movieWithTwoRips
            };
            var movieRip3 = new MovieRip() {
                FileName = "Wake.In.Fright.1971.1080p.BluRay.x264.DD2.0-FGT",
                Movie = movieWithTwoRips
            };
            var movieRip4 = new MovieRip() {
                FileName = "Badlands.1973.1080p.BluRay.X264-AMIABLE",
                Movie = new Movie() { Title = "Badlands", ReleaseDate = 1973 }
            };

            var firstVisit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { movieRip0, movieRip1 },
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
            };
            var secondVisit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { movieRip1, movieRip2 },
                VisitDateTime = DateTime.ParseExact("20220102", "yyyyMMdd", null)
            };
            var thirdVisit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { movieRip3, movieRip4 },
                VisitDateTime = DateTime.ParseExact("20220103", "yyyyMMdd", null)
            };

            this._movieWarehouseVisitRepositoryMock
                .Setup(m => m.GetClosestMovieWarehouseVisit())
                .Returns(thirdVisit);
            this._movieWarehouseVisitRepositoryMock
                .Setup(m => m.GetPreviousMovieWarehouseVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == thirdVisit.VisitDateTime)))
                .Returns(secondVisit);

            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == firstVisit.VisitDateTime)))
                .Returns(firstVisit.MovieRips.Select(mr => mr.Movie));
            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == secondVisit.VisitDateTime)))
                .Returns(secondVisit.MovieRips.Select(mr => mr.Movie));
            this._movieRepositoryMock
                .Setup(m => m.GetAllMoviesInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == thirdVisit.VisitDateTime)))
                .Returns(thirdVisit.MovieRips.Select(mr => mr.Movie));

            // act
            Dictionary<string, IEnumerable<string>> lastVisitDiff = this._scanMoviesManager.GetLastVisitDiff();

            // assert
            using (new AssertionScope())
            {
                lastVisitDiff["removed"].Should().BeEquivalentTo(new string[] { movieRip1.Movie.ToString() });
                lastVisitDiff["added"].Should().BeEquivalentTo(new string[] { movieRip4.Movie.ToString() });
            }
        }

    }
}

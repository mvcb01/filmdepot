using Xunit;
using Moq;
using FluentAssertions;
using System.Threading.Tasks;

using MovieAPIClients;
using MovieAPIClients.Interfaces;
using FilmDomain.Entities;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Helpers;


namespace DepotTests.CRUDTests
{
    public class MovieFinderTests
    {
        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieFinder _movieFinder;

        public MovieFinderTests()
        {
            this._movieAPIClientMock = new Mock<IMovieAPIClient>();
            this._movieFinder = new MovieFinder(this._movieAPIClientMock.Object);
        }

        [Fact]
        public void FindMovieOnlineAsync_WithNoSearchResults_ShouldThrowNoSearchResultsError()
        {
            // arrange
            string titleWithNoResults = "Some movie";
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.IsAny<string>()))
                .ReturnsAsync(new MovieSearchResult[] { });

            // act
            // nada a fazer

            // assert
            this._movieFinder
                .Invoking(m => m.FindMovieOnlineAsync(titleWithNoResults))
                .Should()
                .Throw<NoSearchResultsError>();
        }

        [Fact]
        public async Task FindMovieOnlineAsync_WithSeveralSearchResultsAndWithoutProvidedReleaseDate_ShouldThrowMultipleSearchResultsError()
        {
            // arrange
            string movieTitleToSearch = "the fly";
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1958 },
                };
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.IsAny<string>()))
                .ReturnsAsync(searchResults);

            // act
            // nada a fazer

            // assert
            await this._movieFinder
                .Invoking(m => m.FindMovieOnlineAsync(movieTitleToSearch))
                .Should()
                .ThrowAsync<MultipleSearchResultsError>();
        }

        [Fact]
        public async Task FindMovieOnlineAsync_WithSeveralSearchResultsAndProvidedReleaseDate_ShouldReturnMatchingMovieRip()
        {
            // arrange
            string movieTitleToSearch = "the fly";
            string movieReleaseDateToSearch = "1986";
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1958 },
                };
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(movieTitleToSearch))
                .ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._movieFinder.FindMovieOnlineAsync(movieTitleToSearch, movieReleaseDateToSearch);

            // assert
            // só nos interessa o valor das duas properties Title e ReleaseDate
            movieFound
                .Should()
                .BeEquivalentTo(new { Title = "The Fly", ReleaseDate = 1986 });
        }

        [Fact]
        public async Task FindMovieOnlineAsync_WithProvidedReleaseDate_WithoutDateMatch_ShouldReturnResultWithinDateTolerance()
        {
            // arrange
            string movieTitleToSearch = "adoration";
            string movieReleaseDateToSearch = "2019";
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "Adoration", ReleaseDate = 2013 },
                new MovieSearchResult()  { Title = "Adoration", ReleaseDate = 2020 },
                };
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains(movieTitleToSearch))))
                .ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._movieFinder.FindMovieOnlineAsync(movieTitleToSearch, movieReleaseDateToSearch);

            // assert
            movieFound.Should().BeEquivalentTo(new { Title = "Adoration", ReleaseDate = 2020 });
        }



        [Fact]
        public async Task FindMovieOnlineAsync_WithSeveralSearchResultsAndProvidedReleaseDateWithoutMatch_ShouldThrowNoSearchResultsError()
        {
            // arrange
            string movieTitleToSearch = "the fly";
            string movieReleaseDateToSearch = "1900";
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986, ExternalId = 1 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1958, ExternalId = 2 },
                };
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(movieTitleToSearch))
                .ReturnsAsync(searchResults);

            // act
            // nada a fazer

            // assert
            await this._movieFinder
                .Invoking(m => m.FindMovieOnlineAsync(movieTitleToSearch, movieReleaseDateToSearch))
                .Should()
                .ThrowAsync<NoSearchResultsError>();
        }

        [Fact]
        public async Task FindMovieOnlineAsync_WithSeveralSearchResultsWithSameReleaseDate_ShouldThrowMultipleSearchResultsError()
        {
            // arrange
            string movieTitleToSearch = "the fly";
            string movieReleaseDateToSearch = "1986";
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986, ExternalId = 1 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1986, ExternalId = 2 },
                };
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(movieTitleToSearch))
                .ReturnsAsync(searchResults);

            // act
            // nada a fazer

            // assert
            await this._movieFinder
                .Invoking(m => m.FindMovieOnlineAsync(movieTitleToSearch, movieReleaseDateToSearch))
                .Should()
                .ThrowAsync<MultipleSearchResultsError>();
        }

        [Fact]
        public async Task FindMovieOnlineAsync_WithSeveralSearchResults_ShouldReturnTheResultWithTitleExactMatch()
        {
            // arrange
            string movieTitleToSearch = "sorcerer";
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "Sorcerer", ReleaseDate = 1977},
                new MovieSearchResult()  { Title = "Highlander III: The Sorcerer", ReleaseDate = 1994},
                };
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(movieTitleToSearch))
                .ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._movieFinder.FindMovieOnlineAsync(movieTitleToSearch);

            // assert
            movieFound
                .Should()
                .BeEquivalentTo(new { Title = "Sorcerer", ReleaseDate = 1977 });
        }

        [Fact]
        public async Task FindMovieOnlineAsync_WithSeveralSearchResults_ShouldReturnTheResultWithOriginalTitleExactMatch()
        {
            // arrange
            string movieTitleToSearch = "the fly";
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { OriginalTitle = "The Fly", ReleaseDate = 1986 },
                new MovieSearchResult()  { Title = "Curse of the Fly", ReleaseDate = 1965 },
                };
            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(movieTitleToSearch)).ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._movieFinder.FindMovieOnlineAsync(movieTitleToSearch);

            // assert
            movieFound
                .Should()
                .BeEquivalentTo(new { OriginalTitle = "The Fly", ReleaseDate = 1986 });
        }


    }
}
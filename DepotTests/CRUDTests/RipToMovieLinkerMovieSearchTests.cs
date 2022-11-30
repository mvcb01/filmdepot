using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using System.Linq;
using Polly.Wrap;
using FilmCRUD.CustomExceptions;
using FilmDomain.Entities;
using MovieAPIClients;


namespace DepotTests.CRUDTests
{
    /// <summary>
    /// To test method <c>RipToMovieLinker.SearchMovieAndPickFromResultsAsync</c>.
    /// </summary>
    public class RipToMovieLinkerMovieSearchTests : RipToMovieLinkerTestsBase
    {
        private readonly AsyncPolicyWrap _policyWrap;

        public RipToMovieLinkerMovieSearchTests() : base()
        {
            // used in several tests
            this._policyWrap = this._ripToMovieLinker.GetPolicyWrapFromConfigs(out _);
        }

        [Fact]
        public void SearchMovieAndPickFromResultsAsync_WithNoSearchResults_ShouldThrowNoSearchResultsError()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "Some Movie", ParsedReleaseDate = "1977" };
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Some"))))
                .ReturnsAsync(Enumerable.Empty<MovieSearchResult>);

            // act
            // nothing to do

            // assert
            Func<Task> methodCall = async () => await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);
            methodCall.Should().Throw<NoSearchResultsError>();
        }


        [Fact]
        public void SearchMovieAndPickFromResultsAsync_WithSeveralSearchResultsAndWithoutProvidedReleaseDate_ShouldThrowMultipleSearchResultsError()
        {
            // arrange

            var toSearch = new MovieRip() { ParsedTitle = "The Fly" };

            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1958 },
                };

            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")))).ReturnsAsync(searchResults);

            // act
            // nothing to do

            // assert
            Func<Task> methodCall = async () => await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);
            methodCall.Should().Throw<MultipleSearchResultsError>();
        }

        [Fact]
        public async Task SearchMovieAndPickFromResultsAsync_WithSeveralSearchResultsAndProvidedReleaseDate_ShouldReturnMatchingMovieRip()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Fly", ParsedReleaseDate = "1986" };
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1958 },
                };
            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")))).ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            movieFound.Should().BeEquivalentTo(new { Title = "The Fly", ReleaseDate = 1986 });
        }

        [Fact]
        public async Task SearchMovieAndPickFromResultsAsync_WithProvidedReleaseDate_WithoutDateMatch_ShouldReturnResultWithinDateTolerance()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Death of Dick Long", ParsedReleaseDate = "2019" };
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Death of Dick Long", ReleaseDate = 2013 },
                new MovieSearchResult()  { Title = "The Death of Dick Long", ReleaseDate = 2020 },
                };

            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Long")))).ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            movieFound.Should().BeEquivalentTo(new { Title = "The Death of Dick Long", ReleaseDate = 2020 });
        }

        //[Fact]
        //public void SearchMovieAndPickFromResultsAsync_WithSeveralSearchResultsAndProvidedReleaseDateWithoutMatch_ShouldThrowNoSearchResultsError()
        //{
        //    // arrange
        //    string movieTitleToSearch = "the fly";
        //    string movieReleaseDateToSearch = "1900";
        //    MovieSearchResult[] searchResults = {
        //        new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986, ExternalId = 1 },
        //        new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1958, ExternalId = 2 },
        //        };

        //    // act
        //    // nothing to do

        //    // assert
        //    Action methodCall = () => RipToMovieLinker.SearchMovieAndPickFromResultsAsync(searchResults, movieTitleToSearch, movieReleaseDateToSearch);
        //    methodCall.Should().Throw<NoSearchResultsError>();
        //}

        //[Fact]
        //public void SearchMovieAndPickFromResultsAsync_WithSeveralSearchResultsWithSameReleaseDate_ShouldThrowMultipleSearchResultsError()
        //{
        //    // arrange
        //    string movieTitleToSearch = "the fly";
        //    string movieReleaseDateToSearch = "1986";
        //    MovieSearchResult[] searchResults = {
        //        new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986, ExternalId = 1 },
        //        new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1986, ExternalId = 2 },
        //        };

        //    // act
        //    // nothing to do

        //    // assert
        //    Action methodCall = () => RipToMovieLinker.SearchMovieAndPickFromResultsAsync(searchResults, movieTitleToSearch, movieReleaseDateToSearch);
        //    methodCall.Should().Throw<MultipleSearchResultsError>();
        //}

        //[Fact]
        //public void SearchMovieAndPickFromResultsAsync_WithSeveralSearchResults_ShouldReturnTheResultWithTitleExactMatch()
        //{
        //    // arrange
        //    string movieTitleToSearch = "sorcerer";
        //    MovieSearchResult[] searchResults = {
        //        new MovieSearchResult() { Title = "Sorcerer", ReleaseDate = 1977},
        //        new MovieSearchResult()  { Title = "Highlander III: The Sorcerer", ReleaseDate = 1994},
        //        };

        //    // act
        //    Movie movieFound = RipToMovieLinker.SearchMovieAndPickFromResultsAsync(searchResults, movieTitleToSearch);

        //    // assert
        //    movieFound.Should().BeEquivalentTo(new { Title = "Sorcerer", ReleaseDate = 1977 });
        //}

        //[Fact]
        //public void SearchMovieAndPickFromResultsAsync_WithSeveralSearchResults_ShouldReturnTheResultWithOriginalTitleExactMatch()
        //{
        //    // arrange
        //    string movieTitleToSearch = "the fly";
        //    MovieSearchResult[] searchResults = {
        //        new MovieSearchResult() { OriginalTitle = "The Fly", ReleaseDate = 1986 },
        //        new MovieSearchResult()  { Title = "Curse of the Fly", ReleaseDate = 1965 },
        //        };

        //    // act
        //    Movie movieFound = RipToMovieLinker.SearchMovieAndPickFromResultsAsync(searchResults, movieTitleToSearch);

        //    // assert
        //    movieFound.Should().BeEquivalentTo(new { OriginalTitle = "The Fly", ReleaseDate = 1986 });
        //}
    }
}

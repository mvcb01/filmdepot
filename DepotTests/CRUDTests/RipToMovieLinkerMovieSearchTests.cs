using Xunit;
using Moq;
using FluentAssertions;
using FluentAssertions.Execution;
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

        [Fact]
        public void SearchMovieAndPickFromResultsAsync_WithSeveralSearchResultsAndProvidedReleaseDateWithoutMatch_ShouldThrowNoSearchResultsError()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Fly", ParsedReleaseDate = "1900" };
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986, ExternalId = 101 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1958, ExternalId = 102 },
                };

            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")))).ReturnsAsync(searchResults);

            // act
            // nothing to do

            // assert
            Func<Task> methodCall = async () => await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);
            methodCall.Should().Throw<NoSearchResultsError>();
        }

        [Fact]
        public void SearchMovieAndPickFromResultsAsync_WithSeveralSearchResultsWithSameReleaseDate_ShouldThrowMultipleSearchResultsError()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Fly", ParsedReleaseDate = "1986" };
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "The Fly", ReleaseDate = 1986, ExternalId = 101 },
                new MovieSearchResult()  { Title = "The Fly", ReleaseDate = 1986, ExternalId = 102 },
                };

            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")))).ReturnsAsync(searchResults);

            // act
            // nothing to do

            // assert
            Func<Task> methodCall = async () => await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);
            methodCall.Should().Throw<MultipleSearchResultsError>();
        }

        [Fact]
        public async Task SearchMovieAndPickFromResultsAsync_WithSeveralSearchResults_ShouldReturnTheResultWithTitleExactMatch()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "Sorcerer", ParsedReleaseDate = "1977" };
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { Title = "Sorcerer", ReleaseDate = 1977 },
                new MovieSearchResult()  { Title = "Highlander III: The Sorcerer", ReleaseDate = 1994},
                };

            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Sorcerer")))).ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            movieFound.Should().BeEquivalentTo(new { Title = "Sorcerer", ReleaseDate = 1977 });
        }

        [Fact]
        public async Task SearchMovieAndPickFromResultsAsync_WithSeveralSearchResults_ShouldReturnTheResultWithOriginalTitleExactMatch()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Fly", ParsedReleaseDate = "1986" };
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { OriginalTitle = "The Fly", ReleaseDate = 1986 },
                new MovieSearchResult()  { Title = "Curse of the Fly", ReleaseDate = 1965 },
                };

            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")))).ReturnsAsync(searchResults);

            // act
            Movie movieFound = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            movieFound.Should().BeEquivalentTo(new { OriginalTitle = "The Fly", ReleaseDate = 1986 });
        }


        [Fact]
        public async Task SearchMovieAndPickFromResultsAsync_WithUnparseableReleaseDateString_ShouldCallCorrectApiClientMethodOverload()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Fly", ParsedReleaseDate = "abcd1986xyz--!!" };

            // act
            _ = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            var x = Times.Never;

            // assert
            using (new AssertionScope())
            {
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>()), Times.Once());
            }
        }
    }
}

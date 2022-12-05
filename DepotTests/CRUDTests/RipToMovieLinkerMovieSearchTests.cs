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
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Some")), It.IsAny<int>()))
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
        public async Task SearchMovieAndPickFromResultsAsync_WithProvidedReleaseDate_WithoutDateMatch_ShouldReturnResultWithinDateTolerance()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Death of Dick Long", ParsedReleaseDate = "2019" };

            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Long")), It.Is<int>(i => i != 2020)))
                .ReturnsAsync(Enumerable.Empty<MovieSearchResult>());
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Long")), It.Is<int>(i => i == 2020)))
                .ReturnsAsync(new MovieSearchResult[] { new MovieSearchResult() { Title = "The Death of Dick Long", ReleaseDate = 2020 } });

            // act
            Movie movieFound = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            movieFound.Should().BeEquivalentTo(new { Title = "The Death of Dick Long", ReleaseDate = 2020 });
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

            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")), It.IsAny<int>()))
                .ReturnsAsync(searchResults);

            // act
            // nothing to do...

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

            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Sorcerer")), It.IsAny<int>()))
                .ReturnsAsync(searchResults);

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

            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")), It.IsAny<int>()))
                .ReturnsAsync(searchResults);

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
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { OriginalTitle = "The Fly", ReleaseDate = 1986 },
            };

            this._movieAPIClientMock.Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")))).ReturnsAsync(searchResults);

            // act
            _ = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            using (new AssertionScope())
            {
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never());
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>()), Times.Once());
            }
        }


        [Fact]
        public async Task SearchMovieAndPickFromResultsAsync_WithParseableReleaseDateAndSingleCorrectResult_ShouldCallCorrectApiClientMethodOverloadExactlyOnce()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Fly", ParsedReleaseDate = "1986" };
            MovieSearchResult[] searchResults = {
                new MovieSearchResult() { OriginalTitle = "The Fly", ReleaseDate = 1986 },
            };

            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")), It.Is<int>(i => i == 1986)))
                .ReturnsAsync(searchResults);

            // act
            _ = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            using (new AssertionScope())
            {
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once());
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        public async Task SearchMovieAndPickFromResultsAsync_WithParseableReleaseDateAndNoInitialResults_ShouldCallCorrectApiClientMethodOverloadForCloseDates()
        {
            // arrange
            var toSearch = new MovieRip() { ParsedTitle = "The Fly", ParsedReleaseDate = "1985" };
            MovieSearchResult[] searchResults_1986 = {
                new MovieSearchResult() { OriginalTitle = "The Fly", ReleaseDate = 1986 },
            };

            // will only return results for the correct release date
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")), It.Is<int>(i => i != 1986)))
                .ReturnsAsync(Enumerable.Empty<MovieSearchResult>());
            this._movieAPIClientMock
                .Setup(m => m.SearchMovieAsync(It.Is<string>(s => s.Contains("Fly")), It.Is<int>(i => i == 1986)))
                .ReturnsAsync(searchResults_1986);

            // act
            _ = await this._ripToMovieLinker.SearchMovieAndPickFromResultsAsync(toSearch, this._policyWrap);

            // assert
            using (new AssertionScope())
            {
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>(), It.Is<int>(i => i == 1985)), Times.Once());
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>(), It.Is<int>(i => i != 1985 && Math.Abs(i - 1985) == 1)), Times.AtLeastOnce());
                this._movieAPIClientMock.Verify(m => m.SearchMovieAsync(It.IsAny<string>()), Times.Never());
            }
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
        public void FindRelatedMovieEntityInRepo_WithoutExactMatchesInRepo_ShouldReturnNull()
        {
            // arrange
            var movieRip = new MovieRip()
            {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN",
                ParsedTitle = "the fly",
                ParsedReleaseDate = "1986"
            };

            Movie[] movieMatches = { new Movie() { Title = "The Fly II", ReleaseDate = 1958 } };

            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(It.Is<string>(s => s.Contains("fly"))))
                .Returns(movieMatches);

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntityInRepo(movieRip);

            // assert
            result.Should().BeNull();
        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WhenParsedTitleHasExactTokenMatchInMovieRepository_ShouldReturnTheMatchedMovie()
        {
            // arrange
            var movieRip = new MovieRip()
            {
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
        public void FindRelatedMovieEntityInRepo_WithoutParsedReleaseDate_WithSeveralMatchesInRepo_ShouldThrowMultipleMovieMatchesError()
        {
            // arrange
            var movieRip = new MovieRip()
            {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN",
                ParsedTitle = "the fly",
                ParsedReleaseDate = null
            };
            Movie[] movieMatches = {
                new Movie() { Title = "The Fly", ReleaseDate = 1958 },
                new Movie() { Title = "The Fly", ReleaseDate = 1986 }
            };
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(movieRip.ParsedTitle))
                .Returns(movieMatches);

            // act
            // nothing to do...

            // assert
            this._ripToMovieLinker
                .Invoking(r => r.FindRelatedMovieEntityInRepo(movieRip))
                .Should()
                .Throw<MultipleMovieMatchesError>();
        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WithParsedReleaseDate_WithSeveralTitleTokenMatchesInRepo_ShouldReturnReleaseDateMatch()
        {
            // arrange
            var movieRip = new MovieRip()
            {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN",
                ParsedTitle = "the fly",
                ParsedReleaseDate = "1986"
            };

            var dateMatch = new Movie() { Title = "The Fly", ReleaseDate = 1986 };
            var dateMismatch = new Movie() { Title = "The Fly", ReleaseDate = 1987 };

            Movie[] movieMatches = { dateMatch, dateMismatch };

            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(movieRip.ParsedTitle))
                .Returns(movieMatches);

            // act
            Movie result = this._ripToMovieLinker.FindRelatedMovieEntityInRepo(movieRip);

            //assert
            result.Should().Be(dateMatch);
        }

        [Fact]
        public void FindRelatedMovieEntityInRepo_WithParsedReleaseDate_WithSeveralMatchesInRepoWithDifferentButCloseDates_ShouldThrowMultipleMovieMatchesError()
        {
            // arrange
            var movieRip = new MovieRip()
            {
                FileName = "The.Fly.1986.1080p.BluRay.x264-TFiN",
                ParsedTitle = "the fly",
                ParsedReleaseDate = "1986"
            };
            Movie[] movieMatches = {
                new Movie() { Title = "The Fly", ReleaseDate = 1985 },
                new Movie() { Title = "The Fly", ReleaseDate = 1987 }
            };
            this._movieRepositoryMock
                .Setup(m => m.SearchMoviesWithTitle(movieRip.ParsedTitle))
                .Returns(movieMatches);

            // act
            // nothing to do...

            // assert
            this._ripToMovieLinker
                .Invoking(r => r.FindRelatedMovieEntityInRepo(movieRip))
                .Should()
                .Throw<MultipleMovieMatchesError>();
        }
    }
}

using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using FilmDomain.Extensions;
using FilmDomain.Entities;

namespace DepotTests.FilmDomainTests
{
    public class EntityExtensionsTests
    {
        [Theory]
        [InlineData(null, new string[] {})]
        [InlineData("name surname", new string[] { "name", "surname" })]
        [InlineData("Name Surname", new string[] { "name", "surname" })]
        [InlineData(" namE  suRName  ", new string[] {"name", "surname"})]
        [InlineData(" Some! Name & with Stuff._>  ", new string[] { "some", "name", "with", "stuff" })]
        [InlineData("Béla Tarr", new string[] { "bela", "tarr" })]
        [InlineData("Cação", new string[] { "cacao" })]
        [InlineData("Júli Fàbregas", new string[] { "juli", "fabregas" })]
        [InlineData("Andrés Gertrúdix", new string[] { "andres", "gertrudix" })]
        [InlineData("Göran", new string[] { "goran" })]
        [InlineData("Carlos 'Bochita' Martinetti", new string[] { "carlos", "bochita", "martinetti"})]
        [InlineData("James O'Connell", new string[] { "james", "o", "connell" })]
        [InlineData("Nina Šunevič", new string[] { "nina", "sunevic" })]
        [InlineData("Petr Vaněk", new string[] { "petr", "vanek" })]
        [InlineData("13(tzameti)", new string[] { "13", "tzameti" })]
        public void GetStringTokensWithoutPunctuationAndDiacritics_ShouldReturnCorrectComponents(string name, IEnumerable<string> expected)
        {
            IEnumerable<string> actual = name.GetStringTokensWithoutPunctuation(removeDiacritics: true);
            // order matters
            actual.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }


        [Theory]
        [InlineData(null, new string[] {})]
        [InlineData("name surname", new string[] { "name", "surname" })]
        [InlineData("Name Surname", new string[] { "name", "surname" })]
        [InlineData(" namE  suRName  ", new string[] {"name", "surname"})]
        [InlineData(" Some! Name & with Stuff._>  ", new string[] { "some", "name", "with", "stuff" })]
        [InlineData("Béla Tarr", new string[] { "béla", "tarr" })]
        [InlineData("Cação", new string[] { "cação" })]
        [InlineData("Júli Fàbregas", new string[] { "júli", "fàbregas" })]
        [InlineData("Andrés Gertrúdix", new string[] { "andrés", "gertrúdix" })]
        [InlineData("Göran", new string[] { "göran" })]
        [InlineData("Carlos 'Bochita' Martinetti", new string[] { "carlos", "bochita", "martinetti"})]
        [InlineData("James O'Connell", new string[] { "james", "o", "connell" })]
        [InlineData("Nina Šunevič", new string[] { "nina", "šunevič" })]
        [InlineData("Petr Vaněk", new string[] { "petr", "vaněk" })]
        [InlineData("13(tzameti)", new string[] { "13", "tzameti" })]
        public void GetStringTokensWithoutPunctuation_ShouldReturnCorrectComponents(string name, IEnumerable<string> expected)
        {
            IEnumerable<string> actual = name.GetStringTokensWithoutPunctuation(removeDiacritics: false);
            // order matters
            actual.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Theory]
        [InlineData("benoit _Poelvoorde-->")]
        [InlineData("benoîT     póelvõörDe")]
        [InlineData("[ -> benoîT     póelvõörDe <-]")]
        [InlineData("poelvoorDe")]
        public void GetEntitiesFromNameFuzzyMatching_WithRemoveDiacritics_ShouldReturnCorrectMatches(string nameToSearch)
        {
            // arrange
            var firstCastMember = new CastMember() { Name = "!~~Benoît Pöelvóorde", Id = 0};
            var secondCastMember = new CastMember() { Name = " ([]) - benoit -^^ PoeLVOOrdE *+", Id = 1 };
            var thirdCastMember = new CastMember() { Name = "Zbigniew Zamachowski", Id = 2 };
            var allCastMembers = new CastMember[] { firstCastMember, secondCastMember, thirdCastMember };

            // act
            IEnumerable<CastMember> searchResult = allCastMembers.GetEntitiesFromNameFuzzyMatching(nameToSearch, removeDiacritics: true);

            // assert
            searchResult.Should().BeEquivalentTo(new CastMember[] { firstCastMember, secondCastMember });
        }

        [Theory]
        [InlineData("benoit _Poelvoorde-->")]
        [InlineData("%-(//) benoiT     poelvoorDe")]
        [InlineData("[ -> benoiT     poelvoorDe <-]")]
        [InlineData("poelvoorDe")]
        public void GetEntitiesFromNameFuzzyMatching_WithoutRemoveDiacritics_ShouldReturnCorrectMatches(string nameToSearch)
        {
            // arrange
            var firstCastMember = new CastMember() { Name = "{/% Benoît Pöelvóorde", Id = 0};
            var secondCastMember = new CastMember() { Name = ":!!  benoit /& PoeLVOOrdE ", Id = 1 };
            var thirdCastMember = new CastMember() { Name = "Zbigniew Zamachowski", Id = 2 };
            var allCastMembers = new CastMember[] { firstCastMember, secondCastMember, thirdCastMember };

            // act
            IEnumerable<CastMember> searchResult = allCastMembers.GetEntitiesFromNameFuzzyMatching(nameToSearch, removeDiacritics: false);

            // assert
            searchResult.Should().BeEquivalentTo(new CastMember[] { secondCastMember });

        }

        [Theory]
        [InlineData("$$#!(!)  thE Turin  !!!$$Horsé")]
        [InlineData("túrin horse ")]
        [InlineData("turin horse (2011)")]
        [InlineData("horse 2011")]
        public void GetMoviesFromTitleFuzzyMatching_WithRemoveDiacritics_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { Title = "Sátántangó", ReleaseDate = 1994 };
            var secondMovie = new Movie() { Title = "The Turin Horse", ReleaseDate = 2011 };
            var thirdMovie = new Movie() { Title = "Natural Born Killers", ReleaseDate = 1994 };

            var allMovies = new Movie[] { firstMovie, secondMovie, thirdMovie };

            // act
            IEnumerable<Movie> searchResult = allMovies.GetMovieEntitiesFromTitleFuzzyMatching(title, removeDiacritics: true);

            // assert
            searchResult.Should().BeEquivalentTo(new[] { secondMovie });
        }

        [Theory]
        [InlineData("$$#!(!) Satantango")]
        [InlineData("  saTanTanGo  (1994)")]
        [InlineData("satantango 1994")]
        [InlineData("satantango")]
        public void GetMoviesFromTitleFuzzyMatching_WithoutRemoveDiacritics_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { Title = "Satantango", ReleaseDate = 1994 };
            var secondMovie = new Movie() { Title = "The Turin Horse", ReleaseDate = 2011 };
            var thirdMovie = new Movie() { Title = "Natural Born Killers", ReleaseDate = 1994 };
            var fourthMovie = new Movie() { Title = "Sátántangó", ReleaseDate = 1994 };

            var allMovies = new Movie[] { firstMovie, secondMovie, thirdMovie, fourthMovie };

            // act
            IEnumerable<Movie> searchResult = allMovies.GetMovieEntitiesFromTitleFuzzyMatching(title, removeDiacritics: false);

            // assert
            searchResult.Should().BeEquivalentTo(new[] { firstMovie });
        }

        [Theory]
        [InlineData("dead man's shoes")]
        [InlineData("dead mans shoes")]
        [InlineData("dead man's shoes (2004)")]
        [InlineData("dead mans shoes (2004)")]
        public void GetMoviesFromTitleFuzzyMatching_WithRemoveDiacritics_WithSingleQuotesInMovieTitle_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { Title = "Dead Man's Shoes", ReleaseDate = 2004 };
            var secondMovie = new Movie() { Title = "The Turin Horse", ReleaseDate = 2011 };
            var thirdMovie = new Movie() { Title = "Natural Born Killers", ReleaseDate = 1994 };
            var fourthMovie = new Movie() { Title = "Sátántangó", ReleaseDate = 1994 };

            var allMovies = new Movie[] { firstMovie, secondMovie, thirdMovie, fourthMovie };

            // act
            IEnumerable<Movie> searchResult = allMovies.GetMovieEntitiesFromTitleFuzzyMatching(title, removeDiacritics: false);

            // assert
            searchResult.Should().BeEquivalentTo(new[] { firstMovie });
        }

        [Theory]
        [InlineData("$$#!(!)  A canÇÃo de  !!!$$lisbOA")]
        [InlineData("a canção    de lisboa ")]
        [InlineData("a canção de lisboa (1933)")]
        [InlineData("a cancao de lisboa (1933)")]
        [InlineData("canção de lisboa 1933")]
        [InlineData("cancao de lisboa 1933")]
        public void GetMovieEntitiesFromOriginalTitleFuzzyMatching_WithRemoveDiacritics_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { OriginalTitle = "The Turin Horse", ReleaseDate = 2011 };
            var secondMovie = new Movie() { OriginalTitle = "A Canção de Lisboa", ReleaseDate = 1933 };
            var thirdMovie = new Movie() { OriginalTitle = "Natural Born Killers", ReleaseDate = 1994 };

            var allMovies = new Movie[] { firstMovie, secondMovie, thirdMovie };

            // act
            IEnumerable<Movie> searchResult = allMovies.GetMovieEntitiesFromOriginalTitleFuzzyMatching(title, removeDiacritics: true);

            // assert
            searchResult.Should().BeEquivalentTo(new[] { secondMovie });
        }

        [Theory]
        [InlineData("$$#!(!)  A canCao de  !!!$$lisbOA")]
        [InlineData("a cancao de lisboa (1933)")]
        [InlineData("cancao de lisboa 1933")]
        public void GetMovieEntitiesFromOriginalTitleFuzzyMatching_WithoutRemoveDiacritics_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { OriginalTitle = "The Turin Horse", ReleaseDate = 2011 };
            var secondMovie = new Movie() { OriginalTitle = "A Cancao de Lisboa", ReleaseDate = 1933 };
            var thirdMovie = new Movie() { OriginalTitle = "Natural Born Killers", ReleaseDate = 1994 };
            var fourthMovie = new Movie() { OriginalTitle = "A Canção de Lisboa", ReleaseDate = 1933 };

            var allMovies = new Movie[] { firstMovie, secondMovie, thirdMovie, fourthMovie };

            // act
            IEnumerable<Movie> searchResult = allMovies.GetMovieEntitiesFromOriginalTitleFuzzyMatching(title, removeDiacritics: false);

            // assert
            searchResult.Should().BeEquivalentTo(new[] { secondMovie });
        }

        [Theory]
        [InlineData("dead man's shoes")]
        [InlineData("dead mans shoes")]
        [InlineData("dead man's shoes (2004)")]
        [InlineData("dead mans shoes (2004)")]
        public void GetMovieEntitiesFromOriginalTitleFuzzyMatching_WithRemoveDiacritics_WithSingleQuotesInMovieTitle_ShouldReturnCorrectMatches(string title)
        {
            // arrange
            var firstMovie = new Movie() { OriginalTitle = "Dead Man's Shoes", ReleaseDate = 2004 };
            var secondMovie = new Movie() { OriginalTitle = "The Turin Horse", ReleaseDate = 2011 };
            var thirdMovie = new Movie() { OriginalTitle = "Natural Born Killers", ReleaseDate = 1994 };
            var fourthMovie = new Movie() { OriginalTitle = "Sátántangó", ReleaseDate = 1994 };

            var allMovies = new Movie[] { firstMovie, secondMovie, thirdMovie, fourthMovie };

            // act
            IEnumerable<Movie> searchResult = allMovies.GetMovieEntitiesFromOriginalTitleFuzzyMatching(title, removeDiacritics: false);

            // assert
            searchResult.Should().BeEquivalentTo(new[] { firstMovie });
        }
    }
}

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
        [InlineData("James O'Connell", new string[] { "james", "o'connell" })]
        [InlineData("Nina Šunevič", new string[] { "nina", "sunevic" })]
        [InlineData("Petr Vaněk", new string[] { "petr", "vanek" })]
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
        [InlineData("James O'Connell", new string[] { "james", "o'connell" })]
        [InlineData("Nina Šunevič", new string[] { "nina", "šunevič" })]
        [InlineData("Petr Vaněk", new string[] { "petr", "vaněk" })]
        public void GetStringTokensWithoutPunctuation_ShouldReturnCorrectComponents(string name, IEnumerable<string> expected)
        {
            IEnumerable<string> actual = name.GetStringTokensWithoutPunctuation(removeDiacritics: false);
            // order matters
            actual.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }

        [Fact]
        public void GetEntitiesFromNameFuzzyMatching_WithRemoveDiacritics_ShouldReturnCorrectMatches()
        {
            // arrange
            var firstActor = new Actor() { Name = "Benoît Pöelvóorde", Id = 0};
            var secondActor = new Actor() { Name = " ([]) - benoit -^^ PoeLVOOrdE *+", Id = 1 };
            var thirdActor = new Actor() { Name = "Zbigniew Zamachowski", Id = 2 };
            var allActors = new Actor[] { firstActor, secondActor, thirdActor };

            // act
            IEnumerable<Actor> searchResult = allActors.GetEntitiesFromNameFuzzyMatching("benoit _Poelvoorde-->", removeDiacritics: true);

            // assert
            searchResult.Should().BeEquivalentTo(new Actor[] { firstActor, secondActor });
        }

        [Fact]
        public void GetEntitiesFromNameFuzzyMatching_WithoutRemoveDiacritics_ShouldReturnCorrectMatches()
        {
            // arrange
            var firstActor = new Actor() { Name = "{/% Benoît Pöelvóorde", Id = 0};
            var secondActor = new Actor() { Name = ":!!  benoit /& PoeLVOOrdE ", Id = 1 };
            var thirdActor = new Actor() { Name = "Zbigniew Zamachowski", Id = 2 };
            var allActors = new Actor[] { firstActor, secondActor, thirdActor };

            // act
            IEnumerable<Actor> searchResult = allActors.GetEntitiesFromNameFuzzyMatching("benoit _Poelvoorde-->", removeDiacritics: false);

            // assert
            searchResult.Should().BeEquivalentTo(new Actor[] { secondActor });
        }
    }
}
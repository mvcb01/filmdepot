using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using System.Linq;

using FilmDomain.Extensions;


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
        public void GetStringTokensWithoutPunctuation_ShouldReturnCorrectComponents(string name, IEnumerable<string> expected)
        {
            IEnumerable<string> actual = name.GetStringTokensWithoutPunctuation();
            // order matters
            actual.Should().BeEquivalentTo(expected, opts => opts.WithStrictOrdering());
        }
    }
}

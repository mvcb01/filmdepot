using System;
using System.Collections.Generic;
using System.Linq;
using FilmDomain.Entities;

namespace FilmDomain.Extensions
{
    public static class EntityExtensions
    {
        public static IEnumerable<string> GetFileNames(this IEnumerable<MovieRip> movieRips)
        {
            return movieRips.Select(r => r.FileName);
        }

        public static IEnumerable<DateTime> GetVisitDates(this IEnumerable<MovieWarehouseVisit> visits)
        {
            return visits.Select(v => v.VisitDateTime);
        }

        public static IEnumerable<string> GetStringTokensWithoutPunctuation(this string value)
        {
            if (value == null)
            {
                return Enumerable.Empty<string>();
            }
            var movieTitle = value.Trim().ToLower();
            char[] punctuation = value.Where(Char.IsPunctuation).Distinct().ToArray();
            return movieTitle.Split().Select(s => s.Trim(punctuation));
        }

        public static IEnumerable<string> GetParsedTitleTokens(this MovieRip movieRip)
        {
            return GetStringTokensWithoutPunctuation(movieRip.ParsedTitle);
        }

        public static IEnumerable<string> GetTitleTokens(this Movie movie)
        {
            return GetStringTokensWithoutPunctuation(movie.Title);
        }
    }
}
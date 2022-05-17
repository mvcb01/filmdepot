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

        private static IEnumerable<string> GetStringTokens(string title)
        {
            if (title == null)
            {
                return Enumerable.Empty<string>();
            }
            var movieTitle = title.Trim().ToLower();
            char[] punctuation = title.Where(Char.IsPunctuation).Distinct().ToArray();
            return movieTitle.Split().Select(s => s.Trim(punctuation));
        }

        public static IEnumerable<string> GetParsedTitleTokens(this MovieRip movieRip)
        {
            return GetStringTokens(movieRip.ParsedTitle);
        }

        public static IEnumerable<string> GetTitleTokens(this Movie movie)
        {
            return GetStringTokens(movie.Title);
        }
    }
}
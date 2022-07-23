using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            string valueNormalized = value.Trim().ToLower().Normalize(NormalizationForm.FormC);
            IEnumerable<Char> charsToRemove = valueNormalized.Where(c => !Char.IsLetterOrDigit(c)).Distinct();
            return valueNormalized.Split().Select(s => s.Trim(charsToRemove.ToArray())).Where(s => !string.IsNullOrEmpty(s));
        }

    }
}
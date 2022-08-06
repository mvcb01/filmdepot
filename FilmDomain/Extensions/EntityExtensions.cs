using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FilmDomain.Entities;
using FilmDomain.Interfaces;

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

        public static IEnumerable<string> GetStringTokensWithoutPunctuation(this string value, bool removeDiacritics = false)
        {
            if (value == null)
            {
                return Enumerable.Empty<string>();
            }

            value = value.Trim().ToLower();

            if (removeDiacritics)
            {
                value = RemoveDiacritics(value);
            }

            IEnumerable<Char> charsToRemove = value.Where(c => !Char.IsLetterOrDigit(c)).Distinct();
            return value.Split().Select(s => s.Trim(charsToRemove.ToArray())).Where(s => !string.IsNullOrEmpty(s));
        }

        public static IEnumerable<T> GetEntitiesFromNameFuzzyMatching<T>(
            this IEnumerable<T> allEntities,
            string name,
            bool removeDiacritics = false) where T : INamedEntityWithId
        {
            IEnumerable<string> nameTokensWithoutPunctuation = name.GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics);
            string nameRegex = @"(\s*)(" + string.Join(@")(\s*)(", nameTokensWithoutPunctuation) + @")(\s*)";
            return allEntities.Where(e => Regex.IsMatch(
                string.Join(' ', e.Name.GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics)),
                nameRegex,
                RegexOptions.IgnoreCase));
        }

        public static IEnumerable<Movie> GetMoviesFromTitleFuzzyMatching(
            this IEnumerable<Movie> allMovies,
            string title,
            bool removeDiacritics = false)
        {
            var titleTokensWithoutPunctuation = title.GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics);

            if (!titleTokensWithoutPunctuation.Any())
            {
                return Enumerable.Empty<Movie>();
            }

            string titleRegex = @"(\s*)(" + string.Join(@")(\s*)(", titleTokensWithoutPunctuation) + @")(\s*)";

            IEnumerable<Movie> result = allMovies.Where(m => Regex.IsMatch(
                string.Join(' ', m.Title.GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics)),
                titleRegex,
                RegexOptions.IgnoreCase));

            // if the last token looks like a date that starts with "1" or "2" then we also search
            // for movie entities with such release date
            var lastToken = titleTokensWithoutPunctuation.Last();
            if (Regex.IsMatch(lastToken, "(1|2)([0-9]{3})"))
            {

                IEnumerable<string> titleTokensWithoutPunctuationNoDate = titleTokensWithoutPunctuation.SkipLast(1);
                string titleRegexNoDate = @"(\s*)(" + string.Join(@")(\s*)(", titleTokensWithoutPunctuationNoDate) + @")(\s*)";
                int parsedReleaseDate = int.Parse(lastToken);
                IEnumerable<Movie> extraResults = allMovies.Where(
                    m => m.ReleaseDate == parsedReleaseDate
                        && Regex.IsMatch(
                            string.Join(' ', m.Title.GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics)),
                            titleRegexNoDate,
                            RegexOptions.IgnoreCase));
                result = result.Concat(extraResults);
            }

            return result;
        }

        // taken from
        // https://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net/249126#249126
        public static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString.EnumerateRunes())
            {
                var unicodeCategory = Rune.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string PrettyFormat(this Movie movie)
        {
            string _genres = string.Join(" | ", movie.Genres.Select(g => g.Name));
            string _directors = string.Join(", ", movie.Directors.Select(d => d.Name));
            return string.Join('\n', new string[] {movie.ToString(), _genres, $"Directors: {_directors}"});
        }
    }
}
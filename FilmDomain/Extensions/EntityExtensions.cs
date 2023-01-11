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

        public static IEnumerable<string> GetStringTokensWithoutPunctuation(this string value, bool removeDiacritics = false)
        {
            if (value == null) return Enumerable.Empty<string>();

            value = value.Trim().ToLower();

            if (removeDiacritics)
            {
                value = RemoveDiacritics(value);
            }

            // IDE0039: local function instead of a lambda
            // made static so that variables defined in GetStringTokensWithoutPunctuation are not available inside
            static string CharReplacer(string original, IEnumerable<Char> charsToRemove, char replacement)
            {
                var resultBuilder = new StringBuilder(original);
                foreach (char c in charsToRemove)
                {
                    resultBuilder.Replace(c, replacement);
                }
                return resultBuilder.ToString();
            }

            IEnumerable<Char> charsToRemove = value.Where(c => !Char.IsLetterOrDigit(c)).Distinct();
            IEnumerable<string> tokensWithReplacement = value
                .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
                .Select(s => CharReplacer(s, charsToRemove, ' ').Trim())
                .Where(s => !(string.IsNullOrEmpty(s) || string.IsNullOrWhiteSpace(s)));

            return tokensWithReplacement.Select(s => s.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)).SelectMany(s => s);
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

        /// <summary>
        /// Searches the provided entities <see cref="Movie"/> and returns the entities such that property <see cref="Movie.Title"/>
        /// fuzzy matches the provided parameter <paramref name="title"/>.
        /// </summary>
        /// <param name="allMovies">The entities to search</param>
        /// <param name="title">The title to search</param>
        /// <param name="removeDiacritics">Whether or no to remove diacritics when comparing <paramref name="title"/> and <see cref="Movie.Title"/>.</param>
        /// <returns>The search result</returns>
        public static IEnumerable<Movie> GetMovieEntitiesFromTitleFuzzyMatching(
            this IEnumerable<Movie> allMovies,
            string title,
            bool removeDiacritics = false)
        {
            return SearchMovieEntitiesByFuzzyMatching(allMovies, title, removeDiacritics, m => m.Title);
        }

        /// <summary>
        /// Searches the provided entities <see cref="Movie"/> and returns the entities such that property <see cref="Movie.OriginalTitle"/>
        /// fuzzy matches the provided parameter <paramref name="title"/>.
        /// </summary>
        /// <param name="allMovies">The entities to search</param>
        /// <param name="title">The original title to search</param>
        /// <param name="removeDiacritics">Whether or no to remove diacritics when comparing <paramref name="title"/> and <see cref="Movie.OriginalTitle"/>.</param>
        /// <returns>The search result</returns>
        public static IEnumerable<Movie> GetMovieEntitiesFromOriginalTitleFuzzyMatching(
            this IEnumerable<Movie> allMovies,
            string title,
            bool removeDiacritics = false)
        {
            throw new NotImplementedException();
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
            string _genres = movie.Genres == null ? string.Empty : string.Join(" | ", movie.Genres.Select(g => g.Name));
            string _directors = movie.Directors == null ? string.Empty : string.Join(", ", movie.Directors.Select(d => d.Name));
            string _kwds = movie.Keywords == null ? string.Empty : string.Join(", ", movie.Keywords);
            return string.Join('\n', new string[] {movie.ToString(), _genres, $"Directors: {_directors}", $"IMDB id: {movie.IMDBId}", $"Keywords: {_kwds}" });
        }

        public static string PrettyFormat(this MovieRip movieRip)
        {
            string _id = $"Id: {movieRip.Id}";
            string _filename = $"Filename: {movieRip.FileName}";
            string _parsedTitle = $"ParsedTitle: {movieRip.ParsedTitle}";
            string _parsedReleaseDate = $"ParsedReleaseDate: {movieRip.ParsedReleaseDate}";
            string _parsedRipQuality = $"ParsedRipQuality: {movieRip.ParsedRipQuality}";
            string _parsedRipInfo = $"ParsedRipInfo: {movieRip.ParsedRipInfo}";
            string _parsedRipGroup = $"ParsedRipGroup: {movieRip.ParsedRipGroup}";
            string _linkedMovie = $"Linked movie: {movieRip.Movie}";
            return string.Join('\n', new string[] {
                _id, _filename, _parsedTitle, _parsedReleaseDate,
                _parsedRipQuality, _parsedRipInfo, _parsedRipGroup,
                _linkedMovie });
        }

        /// <summary>
        /// Implements the logic behind extensions <see cref="GetMovieEntitiesFromTitleFuzzyMatching"/> and
        /// <see cref="GetMovieEntitiesFromOriginalTitleFuzzyMatching"/>, while allowing client code to pass parameter
        /// <paramref name="propertyGetter"/> in order to choose the <see cref="Movie"/> property to use on fuzzy
        /// matching.
        /// </summary>
        private static IEnumerable<Movie> SearchMovieEntitiesByFuzzyMatching(
            IEnumerable<Movie> allMovies,
            string title,
            bool removeDiacritics,
            Func<Movie, string> propertyGetter)
        {
            var titleTokensWithoutPunctuation = title.GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics);

            if (!titleTokensWithoutPunctuation.Any())
                return Enumerable.Empty<Movie>();

            string titleRegex = @"(\s*)(" + string.Join(@")(\s*)(", titleTokensWithoutPunctuation) + @")(\s*)";

            IEnumerable<Movie> result = allMovies.Where(m => Regex.IsMatch(
                string.Join(' ', propertyGetter(m).GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics)),
                titleRegex,
                RegexOptions.IgnoreCase));

            // trying matches after removing single quotes
            if (!result.Any())
            {
                result = allMovies.Where(m => Regex.IsMatch(
                    string.Join(' ', propertyGetter(m).Replace("\'", string.Empty).GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics)),
                    titleRegex,
                    RegexOptions.IgnoreCase));
            }

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
                        && (Regex.IsMatch(
                                string.Join(' ', propertyGetter(m).GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics)),
                                titleRegexNoDate,
                                RegexOptions.IgnoreCase)
                            || Regex.IsMatch(
                                string.Join(' ', propertyGetter(m).Replace("\'", string.Empty).GetStringTokensWithoutPunctuation(removeDiacritics: removeDiacritics)),
                                titleRegexNoDate,
                                RegexOptions.IgnoreCase)));
                result = result.Concat(extraResults);
            }

            return result;
        }
    }
}
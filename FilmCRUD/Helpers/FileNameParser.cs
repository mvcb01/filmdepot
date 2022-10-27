using System.Collections.Generic;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using FilmDomain.Entities;
using FilmDomain.Extensions;
using FilmCRUD.CustomExceptions;


namespace FilmCRUD.Helpers
{
    public static class FileNameParser
    {
        // filename tokens will be either separated by "." or by whitespace
        private const string _tokenRegexSplitter = @"(\.|\s)";

        // to split filenames by "720p", "1080p", etc...;
        // examples:
        //      "Cop Car 2015 1080p WEB-DL x264 AC3-JYK" --> "Cop Car 2015", "1080p", "WEB-DL x264 AC3-JYK"
        //      "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]" --> "Khrustalyov My Car 1998", "720p", "BluRay.x264-GHOULS[rarbg]"
        // includes some typos;
        private const string _ripQualityRegexSplitter = @"(720p|1080p|2160p|480p|720pp|10800p|576p)";

        // to split filenames by release type
        private const string _ripReleaseTypeRegexSplitter = @"(WEB-DL|WEBRip|WEB|BluRay|Blu-Ray|HDTV|TVRip|BRRip|BRip|DVDRip|HDRip|BDRip|SCREENER|DVDSCR|DVDSCREENER|XviD|VODRip|R5|DVDR|DVD-Full|DVD-5|DVD-9)";

        // to split by release date without parentheses/brackets/etc...
        // example:
        //      "The Tenant 1976" -> "The Tenant", "1976"
        // second digit only allowed to be one of {8, 9, 0} for obvious reasons
        private const string _releaseDateRegexSplitter = @"(1|2)(8|9|0)([0-9]{2})";

        private const string _parenthesesOrBrackets_Left = @"\(|\[";

        private const string _parenthesesOrBrackets_Right = @"\)|\]";

        public static (string Title, string ReleaseDate) SplitTitleAndReleaseDate(string ParsedTitleAndReleaseDate)
        {
            ParsedTitleAndReleaseDate = ParsedTitleAndReleaseDate.Trim();

            MatchCollection matches = Regex.Matches(
                ParsedTitleAndReleaseDate,
                $"(({_parenthesesOrBrackets_Left})*){_releaseDateRegexSplitter}(({_parenthesesOrBrackets_Right})*)");

            // the only admissible case without matches is when there's no release date
            if (!matches.Any())
            {
                if (!Regex.IsMatch(ParsedTitleAndReleaseDate, $"^([a-z0-9]|{_tokenRegexSplitter})*$", RegexOptions.IgnoreCase))
                {
                    throw new FileNameParserError($"Cannot split into film title and release date: {ParsedTitleAndReleaseDate}");
                }

                return (ParsedTitleAndReleaseDate.Replace('.', ' ').Trim(), null);
            }

            // we consider the date to be in the last match;
            Match releaseDateRegexMatch = matches.OrderBy(m => m.Index).Last();

            string parsedTitle = ParsedTitleAndReleaseDate
                .Substring(0, length: releaseDateRegexMatch.Index)
                .Trim();

            string withoutParsedTitle = ParsedTitleAndReleaseDate
                .Substring(releaseDateRegexMatch.Index, length: releaseDateRegexMatch.Length)
                .Trim();

            string parsedReleasedDate = Regex.Match(withoutParsedTitle, _releaseDateRegexSplitter).Value;

            return (
                string.Join(' ', parsedTitle.GetStringTokensWithoutPunctuation(removeDiacritics: false)),
                string.Join(' ', parsedReleasedDate.GetStringTokensWithoutPunctuation(removeDiacritics: false))
                );
        }

        public static (string RipInfo, string RipGroup) SplitRipInfoAndGroup(string ripInfoAndGroup)
        {
            string ripInfo;
            string ripGroup;

            if (ripInfoAndGroup.Contains('-'))
            {
                IEnumerable<string> components = ripInfoAndGroup
                    .Split('-', StringSplitOptions.TrimEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s));
                ripInfo = string.Join('-', components.SkipLast(1));
                ripGroup = components.Last();
            }
            else
            {
                // cases like {5.1} or [5.1] or (5.1)
                string bracesLeft = @"\{";
                string bracesRight = @"\}";
                bool isSurrounded = Regex.IsMatch(
                    ripInfoAndGroup,
                    $"^({_parenthesesOrBrackets_Left}|{bracesLeft})(.*?)({_parenthesesOrBrackets_Right}|{bracesRight})$",
                    RegexOptions.IgnoreCase);
                if (isSurrounded) return (ripInfoAndGroup, null);

                IEnumerable<string> splittedByTokenSplitter = Regex
                    .Split(ripInfoAndGroup, _tokenRegexSplitter, RegexOptions.IgnoreCase);

                if (splittedByTokenSplitter.Count() > 1)
                {
                    ripGroup = splittedByTokenSplitter.Last();
                    ripInfo = ripInfoAndGroup.Substring(0, ripInfoAndGroup.Length - ripGroup.Length - 1);

                }
                else
                {
                    ripInfo = ripInfoAndGroup;
                    ripGroup = null;
                }
            }

            return (ripInfo, ripGroup);
        }

        public static MovieRip ParseFileNameIntoMovieRip(string fileName)
        {
            string parsedTitleAndReleaseDate;
            string parsedRipInfoAndGroup;
            string parsedRipQuality;
            string parsedRipInfo;
            string parsedRipGroup;

            Match ripQualityMatch = Regex.Match(
                fileName,
                $"(({_parenthesesOrBrackets_Left})*){_ripQualityRegexSplitter}(({_parenthesesOrBrackets_Right})*)",
                RegexOptions.IgnoreCase);

            Match releaseTypeMatch = Regex.Match(
                fileName,
                $"(({_parenthesesOrBrackets_Left})*){_ripReleaseTypeRegexSplitter}(({_parenthesesOrBrackets_Right})*)",
                RegexOptions.IgnoreCase);

            // in this case we assume that param fileName has the format "some movie (1999)" or just the movie title, details
            // are handled by method SplitTitleAndReleaseDate
            if (!ripQualityMatch.Success && !releaseTypeMatch.Success)
            {
                parsedTitleAndReleaseDate = fileName;
                parsedRipQuality = parsedRipInfoAndGroup = null;
            }
            // cases with rip quality like "Some.Movie.1998.720p.BluRay.x264-GHOULS[rarbg]"
            else if (ripQualityMatch.Success)
            {
                parsedTitleAndReleaseDate = fileName.Substring(0, length: ripQualityMatch.Index).Trim();
                parsedRipQuality = Regex.Replace(
                    Regex.Replace(
                        ripQualityMatch.Value,
                        _parenthesesOrBrackets_Left,
                        string.Empty),
                    _parenthesesOrBrackets_Right,
                    string.Empty);

                parsedRipInfoAndGroup = fileName.Substring(ripQualityMatch.Index + ripQualityMatch.Length).Trim('.', ' ');
            }
            // cases where the filename does not contain rip quality but contains release type
            // example: "The.Wicker.Man.1973.WEB - DL.XviD.MP3 - RARBG"
            else
            {
                parsedTitleAndReleaseDate = fileName.Substring(0, length: releaseTypeMatch.Index).Trim();
                parsedRipQuality = null;

                parsedRipInfoAndGroup = fileName.Substring(releaseTypeMatch.Index).Trim('.', ' ');
            }

            var (parsedTitle, parsedReleaseDate) = SplitTitleAndReleaseDate(parsedTitleAndReleaseDate);
            (parsedRipInfo, parsedRipGroup) =  string.IsNullOrEmpty(parsedRipInfoAndGroup) ? (null, null) : SplitRipInfoAndGroup(parsedRipInfoAndGroup);
            return new MovieRip() {
                FileName = fileName.Trim(),
                ParsedTitle = parsedTitle.Trim(),
                ParsedReleaseDate = parsedReleaseDate?.Trim(),
                ParsedRipQuality = parsedRipQuality,
                ParsedRipInfo = parsedRipInfo,
                ParsedRipGroup = parsedRipGroup
            };
        }

        public static MovieRip ParseFileNameIntoMovieRip_OLD(string fileName)
        {
            string parsedRipQuality;
            string parsedRipInfo;
            string parsedRipGroup;

            IEnumerable<string> split;

            IEnumerable<string> splitByQuality = Regex
                .Split(fileName, $"{_tokenRegexSplitter}{_ripQualityRegexSplitter}(({_tokenRegexSplitter})*)", RegexOptions.IgnoreCase)
                .Where(s => s != "." && !string.IsNullOrWhiteSpace(s));
            int splitByQualityCount = splitByQuality.Count();

            IEnumerable<string> splitByReleaseType = Regex
                .Split(fileName, $"{_tokenRegexSplitter}{_ripReleaseTypeRegexSplitter}(({_tokenRegexSplitter})*)", RegexOptions.IgnoreCase)
                .Where(s => s != "." && !string.IsNullOrWhiteSpace(s));
            int splitByReleaseTypeCount = splitByReleaseType.Count();

            // most likely this will be true when the filename only contains title and release date, like "Ex Drummer (2007)"
            if (splitByQualityCount == 1 && splitByReleaseTypeCount == 1)
            {
                parsedRipQuality = null;
                parsedRipInfo = null;
                parsedRipGroup = null;

                split = splitByQuality;
            }
            // cases like "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]"
            else if (splitByQualityCount > 1)
            {
                parsedRipQuality = splitByQuality.Skip(1).First().Trim();
                string parsedRipInfoAndGroup = splitByQuality.Skip(2).FirstOrDefault()?.Trim();

                (parsedRipInfo, parsedRipGroup) = parsedRipInfoAndGroup != null ? SplitRipInfoAndGroup(parsedRipInfoAndGroup) : (null, null);

                split = splitByQuality;
            }
            // when the filename does not contain rip quality but contains origin
            // example: "The.Wicker.Man.1973.WEB - DL.XviD.MP3 - RARBG"
            else if (splitByReleaseTypeCount > 1)
            {
                parsedRipQuality = null;
                string parsedRipInfoAndGroup = string.Join(' ', splitByReleaseType.Skip(1));

                (parsedRipInfo, parsedRipGroup) = SplitRipInfoAndGroup(parsedRipInfoAndGroup);

                split = splitByReleaseType;
            }
            else throw new FileNameParserError($"Cannot split into rip components, please add components manually: {fileName}");

            var (title, releaseDate) = SplitTitleAndReleaseDate(split.First());

            return new MovieRip() {
                FileName = fileName.Trim(),
                ParsedTitle = title.Trim(),
                ParsedReleaseDate = releaseDate?.Trim(),
                ParsedRipQuality = parsedRipQuality,
                ParsedRipInfo = parsedRipInfo,
                ParsedRipGroup = parsedRipGroup
            };
        }

    }
}
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FilmDomain.Entities;
using FilmCRUD.CustomExceptions;
using System;

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
        private const string _ripReleaseTypeRegexSplitter = @"(WEB-DL|WEBRip|WEB|BluRay|Blu-Ray|HDTV|TVRip|BRRip|BrRip|BRip|DVDRip|HDRip|BDRip|SCREENER|DVDSCR|DVDSCREENER|XviD|VODRip|R5|DVDR|DVD-Full|DVD-5|DVD-9)";

        // to split by release date without parentheses/brackets/etc...
        // example:
        //      "The Tenant 1976" -> "The Tenant", "1976"
        private const string _titleAndReleaseDateSplitter_Raw = @"(1|2)([0-9]{3})";

        // to split by release date with parentheses
        // example:
        //      "The Tenant (1976)" -> "The Tenant", "1976"
        private const string _titleAndReleaseDateSplitter_WithParenth = @"\((1|2)([0-9]{3})\)";

        // to split by release date with brackets
        // example:
        //      "The Tenant [1976]" -> "The Tenant", "1976"
        private const string _titleAndReleaseDateSplitter_WithBrackets = @"\[(1|2)([0-9]{3})\]";

        public static (string Title, string ReleaseDate) SplitTitleAndReleaseDate(string ParsedTitleAndReleaseDate)
        {
            ParsedTitleAndReleaseDate = ParsedTitleAndReleaseDate.Trim();

            MatchCollection matches = Regex.Matches(
                ParsedTitleAndReleaseDate,
                $"{_titleAndReleaseDateSplitter_Raw}|{_titleAndReleaseDateSplitter_WithParenth}|{_titleAndReleaseDateSplitter_WithBrackets}");

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
                .Replace('.', ' ')
                .Trim();

            string withoutParsedTitle = ParsedTitleAndReleaseDate
                .Substring(releaseDateRegexMatch.Index, length: releaseDateRegexMatch.Length)
                .Replace('.', ' ')
                .Trim();

            string parsedReleasedDate = Regex.Match(withoutParsedTitle, _titleAndReleaseDateSplitter_Raw).Value;

            // finds the date considering both scenarios: "1978" and "1978.REMASTERED
            if (!int.TryParse(parsedReleasedDate, out _)) throw new FileNameParserError($"Cannot find release date: {parsedReleasedDate}");

            return (parsedTitle, parsedReleasedDate);
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
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
        private const string _ripQualityRegexSplitter = @"(720p|1080p|2160p|BRRip|480p|DVDRip|HDRip|BDRip|720pp|10800p|576p)";

        // to split by release date without parentheses
        // example:
        //      "Cop Car 2015" -> "Cop Car", "2015"
        private const string _titleAndReleaseDateSplitter_WithoutParenth = @"(1|2)([0-9]{3})";

        // to split by release date with parentheses
        // example:
        //      "The Tragedy Of Macbeth (2021)" -> "The Tragedy Of Macbeth", "2021"
        private const string _titleAndReleaseDateSplitter_WithParenth = @"\((1|2)([0-9]{3})\)";

        // matches any word with chars "a", ...., "z", "-"; also matches the empty word
        private const string _anyLetterSequencePlusChars = @"([a-z]|-)*";

        // considers both split possibilities - with and without parentheses - allowing for a trailing token sequence
        private static string _titleAndReleaseDateSplitter
        {
            get => $"((({_titleAndReleaseDateSplitter_WithParenth})|({_titleAndReleaseDateSplitter_WithoutParenth}))({_tokenRegexSplitter}{_anyLetterSequencePlusChars})*)$";
        }

        public static (string Title, string ReleaseDate) SplitTitleAndReleaseDate(string ParsedTitleAndReleaseDate)
        {
            ParsedTitleAndReleaseDate = ParsedTitleAndReleaseDate.Trim();

            List<Match> matches = Regex
                .Matches(ParsedTitleAndReleaseDate.Trim(), _titleAndReleaseDateSplitter, RegexOptions.IgnoreCase)
                .ToList();

            if (!matches.Any()) throw new FileNameParserError($"Cannot split into film title and release date: {ParsedTitleAndReleaseDate}");

            // we consider the date to be in the last match;
            // may be like "1978" or like "1978.REMASTERED"
            Match releaseDateMatch = matches.OrderBy(m => m.Index).Last();

            string parsedTitle = ParsedTitleAndReleaseDate
                .Substring(0, length: releaseDateMatch.Index)
                .Replace('.', ' ')
                .Trim();
            string parsedReleasedDate = ParsedTitleAndReleaseDate
                .Substring(releaseDateMatch.Index, releaseDateMatch.Length)
                .Replace('.', ' ')
                .Trim();

            if (parsedReleasedDate.StartsWith("(") & parsedReleasedDate.EndsWith(")"))
            {
                parsedReleasedDate = parsedReleasedDate.Substring(1, parsedReleasedDate.Length - 2);
            }

            // finds the date considering both scenarios: "1978" and "1978.REMASTERED"
            bool isNumeric = int.TryParse(parsedReleasedDate, out _);
            if (!isNumeric)
            {
                Match firstTokenMatch = Regex.Matches(parsedReleasedDate, _tokenRegexSplitter).First();
                parsedReleasedDate = parsedReleasedDate.Substring(0, firstTokenMatch.Index);

                // should be numeric
                if (!int.TryParse(parsedReleasedDate, out _)) throw new FileNameParserError($"Cannot find release date: {parsedReleasedDate}");
            }

            return (parsedTitle, parsedReleasedDate);
        }

        public static List<string> SplitRipInfoAndGroup(string ripInfoAndGroup)
        {
            string ripInfo;
            string ripGroup;

            if (ripInfoAndGroup.Contains('-'))
            {
                var components = ripInfoAndGroup.Split('-', StringSplitOptions.TrimEntries).ToList();
                ripInfo = string.Join('-', components.SkipLast(1));
                ripGroup = components.Last();
            }
            else
            {
                List<string> splittedByTokenSplitter = Regex
                    .Split(ripInfoAndGroup, _tokenRegexSplitter, RegexOptions.IgnoreCase)
                    .ToList();
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

            return new List<string>() { ripInfo, ripGroup };
        }

        public static MovieRip ParseFileNameIntoMovieRip(string fileName)
        {
            List<string> splitted = Regex
                .Split(fileName, $"{_tokenRegexSplitter}{_ripQualityRegexSplitter}{_tokenRegexSplitter}", RegexOptions.IgnoreCase)
                .ToList().Except(new List<string>() {".", " "}).ToList();

            string parsedRipQuality;
            string parsedRipInfo;
            string parsedRipGroup;

            // cases where the filename only contains title and release date, like "Ex Drummer (2007)"
            if (splitted.Take(2).Count() == 1)
            {
                parsedRipQuality = null;
                parsedRipInfo = null;
                parsedRipGroup = null;
            }
            // cases like "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]"
            else if (splitted.Count() == 3)
            {
                parsedRipQuality = splitted[1].Trim();
                string parsedRipInfoAndGroup = splitted[2].Trim();

                List<string> RipInfoAndGroupSplitted = SplitRipInfoAndGroup(parsedRipInfoAndGroup);
                parsedRipInfo = RipInfoAndGroupSplitted[0];
                parsedRipGroup = RipInfoAndGroupSplitted[1];
            }
            else throw new FileNameParserError($"Cannot split: {fileName}");

            var (title, releaseDate) = SplitTitleAndReleaseDate(splitted[0]);

            return new MovieRip() {
                FileName = fileName.Trim(),
                ParsedTitle = title.Trim(),
                ParsedReleaseDate = releaseDate.Trim(),
                ParsedRipQuality = parsedRipQuality,
                ParsedRipInfo = parsedRipInfo,
                ParsedRipGroup = parsedRipGroup
            };
        }

    }
}
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
        private const string _tokenRegexSplitter = @"(\.|\s)";

        // para fazer split ao filename por "720p" ou "1080p" ou outros, por ex:
        //      "Cop Car 2015 1080p WEB-DL x264 AC3-JYK" --> "Cop Car 2015", "1080p", "WEB-DL x264 AC3-JYK"
        // ou ainda
        //      "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]" --> "Khrustalyov My Car 1998", "720p", "BluRay.x264-GHOULS[rarbg]"
        private const string _ripQualityRegexSplitter = @"(720p|1080p|2160p|BRRip|480p|DVDRip|HDRip|BDRip|720pp|10800p|576p)";

        // para fazer split pela release date sem parênteses:
        //      "Cop Car 2015" -> "Cop Car", "2015"
        private const string _titleAndReleaseDateSplitter_WithoutParenth = @"(1|2)([0-9]{3})";

        // para fazer split pela release date com parênteses:
        //      "The Tragedy Of Macbeth (2021)" -> "The Tragedy Of Macbeth", "2021"
        private const string _titleAndReleaseDateSplitter_WithParenth = @"\((1|2)([0-9]{3})\)";

        // qualquer palavra com letras a-z, incluindo a palavra vazia, e alguns chars extra
        private static string _anyLetterSequencePlusChars { get { return $"([a-z]|-)*";}}

        // junta as duas possibilidades de split - com e sem parenteses - permitindo uma sequência de chars alfabeticos
        // no fim, eventualmente separados pelo TokenRegexSplitter
        private static string _titleAndReleaseDateSplitter
        {
            get { return $"((({_titleAndReleaseDateSplitter_WithParenth})|({_titleAndReleaseDateSplitter_WithoutParenth}))({_tokenRegexSplitter}{_anyLetterSequencePlusChars})*)$"; }
        }

        public static List<string> SplitTitleAndReleaseDate(string ParsedTitleAndReleaseDate)
        {
            ParsedTitleAndReleaseDate = ParsedTitleAndReleaseDate.Trim();

            List<Match> matches = Regex.Matches(
                ParsedTitleAndReleaseDate.Trim(),
                _titleAndReleaseDateSplitter,
                RegexOptions.IgnoreCase
                ).ToList();

            if (matches.Count() == 0)
            {
                throw new FileNameParserError("Cannot split into film title and release date: " + ParsedTitleAndReleaseDate);
            }

            // consideramos que a data está no último match, que pode ser de uma das formas:
            //      1978
            // ou
            //      1978.REMASTERED
            Match releaseDateMatch = matches.OrderBy(m => m.Index).Last();

            string parsedTitle = ParsedTitleAndReleaseDate
                .Substring(0, length: releaseDateMatch.Index)
                .Replace('.', ' ').Trim();
            string parsedReleasedDate = ParsedTitleAndReleaseDate
                .Substring(releaseDateMatch.Index, releaseDateMatch.Length)
                .Replace('.', ' ').Trim();

            if (parsedReleasedDate.StartsWith("(") & parsedReleasedDate.EndsWith(")"))
            {
                parsedReleasedDate = parsedReleasedDate.Substring(1, parsedReleasedDate.Length - 2);
            }

            // vê em qual dos casos cai, se for como "1978.REMASTERED" então vai buscar só a data
            bool isNumeric = int.TryParse(parsedReleasedDate, out _);
            if (!isNumeric)
            {
                Match firstTokenMatch = Regex.Matches(parsedReleasedDate, _tokenRegexSplitter).First();
                parsedReleasedDate = parsedReleasedDate.Substring(0, firstTokenMatch.Index);

                // aqui já deve ser numérico
                if (!int.TryParse(parsedReleasedDate, out _))
                {
                    throw new FileNameParserError("Cannot find release date: " + parsedReleasedDate);
                }
            }

            return new List<string>() { parsedTitle, parsedReleasedDate };
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

            // casos em que o ficheiro apenas tem o nome e data, por ex "Ex Drummer (2007)"
            if (splitted.Count() == 1)
            {
                parsedRipQuality = null;
                parsedRipInfo = null;
                parsedRipGroup = null;
            }
            // casos mais frequentes como "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]"
            else if (splitted.Count() == 3)
            {
                parsedRipQuality = splitted[1].Trim();
                string parsedRipInfoAndGroup = splitted[2].Trim();

                List<string> RipInfoAndGroupSplitted = SplitRipInfoAndGroup(parsedRipInfoAndGroup);
                parsedRipInfo = RipInfoAndGroupSplitted[0];
                parsedRipGroup = RipInfoAndGroupSplitted[1];
                {}
            }
            else
            {
                throw new FileNameParserError($"Cannot split: {fileName}");
            }

            var titleAndRelaseDate = SplitTitleAndReleaseDate(splitted[0]);

            return new MovieRip() {
                FileName = fileName.Trim(),
                ParsedTitle = titleAndRelaseDate[0].Trim(),
                ParsedReleaseDate = titleAndRelaseDate[1].Trim(),
                ParsedRipQuality = parsedRipQuality,
                ParsedRipInfo = parsedRipInfo,
                ParsedRipGroup = parsedRipGroup
            };
        }

    }
}
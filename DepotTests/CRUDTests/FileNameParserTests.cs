using Xunit;
using FluentAssertions;
using FilmDomain.Entities;
using FilmCRUD.Helpers;
using FluentAssertions.Execution;

namespace DepotTests.CRUDTests
{
    public class FileNameParserTests
    {
        [Theory]
        [InlineData(
            "The.Deer.Hunter.1978.REMASTERED.1080p.BluRay.x264.DTS-HD.MA.5.1-FGT",
            "The Deer Hunter",
            "1978",
            "1080p",
            "BluRay.x264.DTS-HD.MA.5.1",
            "FGT")]
        [InlineData(
            "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
            "khrustalyov my car",
            "1998",
            "720p",
            "BluRay.x264",
            "GHOULS[rarbg]")]
        [InlineData(
            "Sicario 2015 1080p BluRay x264 AC3-JYK",
            "sicario",
            "2015",
            "1080p",
            "BluRay x264 AC3",
            "JYK")]
        [InlineData(
            "The.Lives.of.Others.2006.GERMAN.REMASTERED.1080p.BluRay.x264.DTS-NOGRP",
            "the lives of others",
            "2006",
            "1080p",
            "BluRay.x264.DTS",
            "NOGRP")]
        [InlineData(
            "Terminator.2.Judgement.Day.1991.Extended.REMASTERED.1080p.BluRay.H264.AAC.READ.NFO-RARBG",
            "terminator 2 judgement day",
            "1991",
            "1080p",
            "BluRay.H264.AAC.READ.NFO",
            "RARBG")]
        [InlineData(
            "A.Hero.2021.1080p.AMZN.WEBRip.DDP5.1.x264-TEPES",
            "A Hero",
            "2021",
            "1080p",
            "AMZN.WEBRip.DDP5.1.x264",
            "TEPES")]
        [InlineData(
            "Nosferatu.The.Vampyre.1979.FESTiVAL.DVDRip.XviD-NODLABS",
            "nosferatu the vampyre",
            "1979",
            null,
            "DVDRip.XviD",
            "NODLABS")]
        [InlineData(
            "Ex Drummer (2007)",
            "ex drummer",
            "2007",
            null,
            null,
            null)]
        [InlineData(
            "Idiocracy.2006.WEB-DL.1080p.x264.anoXmous",
            "idiocracy",
            "2006",
            "1080p",
            "x264",
            "anoXmous")]
        [InlineData(
            "The.Wicker.Man.1973.WEB - DL.XviD.MP3 - RARBG",
            "the wicker man",
            "1973",
            null,
            "WEB-DL.XviD.MP3",
            "RARBG")]
        [InlineData(
            "Straw.Dogs.720p.BluRay.x264.KickASS",
            "straw dogs",
            null,
            "720p",
            "BluRay.x264",
            "KickASS")]
        [InlineData(
            "underground",
            "underground",
            null,
            null,
            null,
            null)]
        [InlineData(
            "The Omen [1976] 1080p BluRay AAC x264-ETRG",
            "the omen",
            "1976",
            "1080p",
            "BluRay AAC x264",
            "ETRG")]
        [InlineData(
            "Motel Hell 1980 720p - BRRip -MRShanku Silver RG",
            "motel hell",
            "1980",
            "720p",
            "BRRip",
            "MRShanku Silver RG")]
        [InlineData(
            "The Grey (2012) Ita-Eng 720p Bluray x264 -L@ZyMaN",
            "the grey",
            "2012",
            "720p",
            "Bluray x264",
            "L@ZyMaN")]
        [InlineData(
            "Arena.[2009].Jo�o.Salaviza.DVDRip",
            "arena",
            "2009",
            null,
            "DVDRip",
            null)]
        [InlineData(
            "A Prophet [2009] 720p",
            "a prophet",
            "2009",
            "720p",
            null,
            null)]
        [InlineData(
            "Nil By Mouth DVDRip",
            "nil by mouth",
            null,
            null,
            "DVDRip",
            null)]
        [InlineData(
            "1922.2017.1080p.NF.WEB-DL.DD5.1.x264-NTG[EtHD]",
            "1922",
            "2017",
            "1080p",
            "NF.WEB-DL.DD5.1.x264",
            "NTG[EtHD]")]
        [InlineData(
            "2001.A.Space.Odyssey.1968.1080p.BluRay.DTS.x264-HaB [PublicHD.ORG]",
            "2001 a space odyssey",
            "1968",
            "1080p",
            "BluRay.DTS.x264",
            "HaB [PublicHD.ORG]")]
        [InlineData(
            "Die Hard (1988) [1080p] {5.1}",
            "die hard",
            "1988",
            "1080p",
            "{5.1}",
            null)]
        [InlineData(
            "Die Hard 2 (1990) [1080p] {5.1}",
            "die hard 2",
            "1990",
            "1080p",
            "{5.1}",
            null)]
        [InlineData(
            "The Endless (2017) [1080p] [YTS.ME]",
            "the endless",
            "2017",
            "1080p",
            "[YTS.ME]",
            null)]
        [InlineData(
            "The Pirate Bay Away From Keyboard (2013) [1080p]",
            "the pirate bay away from keyboard",
            "2013",
            "1080p",
            null,
            null)]
        [InlineData(
            "The Jacket (2005) [DvdRip] [Xvid] {1337x}-Noir",
            "the jacket",
            "2005",
            null,
            "[DvdRip] [Xvid] {1337x}",
            "Noir")]
        [InlineData(
            "Rambo First Blood (1982) [1920x1080] [Phr0stY]",
            "rambo first blood",
            "1982",
            "1920x1080",
            "[Phr0stY]",
            null)]
        public void ParseFileNameIntoMovieRip_ShouldReturnCorrectComponents(
            string fileName,
            string title,
            string releasedDate,
            string ripQuality,
            string ripInfo,
            string ripGroup)
        {
            MovieRip actual = FileNameParser.ParseFileNameIntoMovieRip(fileName);

            using (new AssertionScope())
            {
                title.Should().BeEquivalentTo(actual.ParsedTitle);
                releasedDate.Should().BeEquivalentTo(actual.ParsedReleaseDate);
                ripQuality.Should().BeEquivalentTo(actual.ParsedRipQuality);
                ripInfo.Should().BeEquivalentTo(actual.ParsedRipInfo);
                ripGroup.Should().BeEquivalentTo(actual.ParsedRipGroup);
            }
        }

        [Theory]
        [InlineData("The Tragedy Of Macbeth (2021)", "the tragedy of macbeth", "2021")]
        [InlineData("Cop Car 2015 ", "Cop Car", "2015")]
        [InlineData("  Khrustalyov.My.Car.1998", "khrustalyov my car", "1998")]
        [InlineData(" Serpico    ", "Serpico", null)]
        [InlineData("The Tenant [1976]", "the tenant", "1976")]
        [InlineData("The.Tenant[1976]", "the tenant", "1976")]
        [InlineData("1922[2017]", "1922", "2017")]
        [InlineData("1922 2017", "1922", "2017")]
        [InlineData("1922.2017", "1922", "2017")]
        [InlineData("2001 A Space Odyssey 1968", "2001 a space odyssey", "1968")]
        [InlineData("2001 A Space Odyssey[1968]", "2001 a space odyssey", "1968")]
        [InlineData("2001.A.Space.Odyssey.1968", "2001 a space odyssey", "1968")]
        [InlineData("Blade Runner 2049 2017", "blade runner 2049", "2017")]
        [InlineData("Blade Runner 2049 (2017)", "blade runner 2049", "2017")]
        [InlineData("Blade.Runner.2049.2017", "blade runner 2049", "2017")]
        [InlineData("Blade Runner 2049[2017]", "blade runner 2049", "2017")]
        public void SplitTitleAndReleaseDate_ShouldReturnCorrectTitleAndReleaseDate(
            string titleAndRelaseDate,
            string expectedTitle,
            string expectedReleaseDate)
        {
            var (actualTitle, actualReleaseDate) = FileNameParser.SplitTitleAndReleaseDate(titleAndRelaseDate);

            using (new AssertionScope())
            {
                // BeEquivalentTo - ignores case and leading/trailing whitespaces
                actualTitle.Should().BeEquivalentTo(expectedTitle);
                actualReleaseDate.Should().BeEquivalentTo(expectedReleaseDate);
            } 
        }

        [Theory]
        [InlineData("BluRay.x264-GECKOS", "BluRay.x264", "GECKOS")]
        [InlineData("BluRay.H264.AAC-VXT", "BluRay.H264.AAC", "VXT")]
        [InlineData("BluRay x264 DTS-JYK", "BluRay x264 DTS", "JYK")]
        [InlineData("BluRay.x264.DTS-HD.MA.5.1-FGT", "BluRay.x264.DTS-HD.MA.5.1", "FGT")]
        [InlineData("BluRay x264 Mayan AAC - Ozlem", "BluRay x264 Mayan AAC", "Ozlem")]
        [InlineData("BluRay.x264.anoXmous", "BluRay.x264", "anoXmous")]
        [InlineData("BDRip.XviD-Larceny", "BDRip.XviD", "Larceny")]
        [InlineData("[DvdRip] [Xvid] {1337x}-Noir", "[DvdRip] [Xvid] {1337x}", "Noir")]
        [InlineData("BluRay 5.1 Ch x265 HEVC SUJAIDR", "BluRay 5.1 Ch x265 HEVC", "SUJAIDR")]
        public void SplitRipInfoAndGroup_ShouldReturnCorrectInfoAndGroup(
            string ripInfoAndGroup,
            string expectedRipInfo,
            string expectedRipGroup)
        {
            var (actualRipInfo, actualRipGroup) = FileNameParser.SplitRipInfoAndGroup(ripInfoAndGroup);

            using (new AssertionScope())
            {
                actualRipInfo.Should().BeEquivalentTo(expectedRipInfo);
                actualRipGroup.Should().BeEquivalentTo(expectedRipGroup);
            }
        }

    }
}
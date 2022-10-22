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
            "DVDRip XviD",
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
            "WEB-DL XviD MP3",
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
            "Arena.[2009].João.Salaviza.DVDRip",
            "arena",
            "2009",
            null,
            "DVDRip",
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
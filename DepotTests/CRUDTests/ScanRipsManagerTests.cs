using Xunit;
using Moq;
using System.Collections.Generic;
using System.Linq.Expressions;
using FluentAssertions;
using System;
using System.Linq;

using FilmCRUD;
using FilmDomain.Interfaces;
using FilmDomain.Entities;
using FluentAssertions.Execution;

namespace DepotTests.CRUDTests
{
    public class ScanRipsManagerTests
    {
        private readonly Mock<IMovieRipRepository> _movieRipRepositoryMock;

        private readonly  Mock<IMovieWarehouseVisitRepository> _movieWarehouseVisitRepositoryMock;

        private readonly  Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly ScanRipsManager _scanRipsManager;

        public ScanRipsManagerTests()
        {
            this._movieRipRepositoryMock = new Mock<IMovieRipRepository>(MockBehavior.Strict);
            this._movieWarehouseVisitRepositoryMock = new Mock<IMovieWarehouseVisitRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.MovieWarehouseVisits)
                .Returns(this._movieWarehouseVisitRepositoryMock.Object);

            this._unitOfWorkMock
                .SetupGet(u => u.MovieRips)
                .Returns(this._movieRipRepositoryMock.Object);

            this._scanRipsManager = new ScanRipsManager(this._unitOfWorkMock.Object);
        }

        [Fact]
        public void GetRipCountByReleaseDate_ReturnsCorrectCount()
        {
            // arrange
            var visit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() {
                    new MovieRip() { ParsedReleaseDate = "1999" },
                    new MovieRip() { ParsedReleaseDate = "1999" },
                    new MovieRip() { ParsedReleaseDate = "2000" }
                }
            };

            // act
            var countByReleaseDate = this._scanRipsManager.GetRipCountByReleaseDate(visit);

            // assert
            var expected = new Dictionary<string, int>() {
                ["1999"] = 2,
                ["2000"] = 1
            };
            countByReleaseDate.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetAllRipsWithReleaseDate_ReturnsExpectedFileNames()
        {
            // arrange
            var releaseDates = new int[] { 1997, 1998 };
            var movieRips = new List<MovieRip>() {
                    new MovieRip() {
                        FileName = "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                        ParsedReleaseDate = "1997"
                        },
                     new MovieRip() {
                        FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                        ParsedReleaseDate = "1997"
                        },
                    new MovieRip() {
                        FileName = "Papillon.1973.1080p.BluRay.X264-AMIABLE",
                        ParsedReleaseDate = "1973"
                        }
                };
            var visit = new MovieWarehouseVisit() { MovieRips = movieRips };

            // act
            var ripsWithReleaseDate = this._scanRipsManager.GetAllRipsWithReleaseDate(visit, releaseDates.ToArray());

            // assert
            var expected = new List<string>() {
                "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                "Gummo.1997.DVDRip.XviD-DiSSOLVE"
            };
            // da documentação:
            //     The two collections are equivalent when they both contain the same strings in any order.
            ripsWithReleaseDate.Should().BeEquivalentTo(expected);

            // garante que não foi executado o método IMovieRipRepository.Find
            _movieRipRepositoryMock.Verify(m => m.Find(It.IsAny<Expression<Func<MovieRip, bool>>>()), Times.Never);
        }

        [Fact]
        public void GetRipCountByVisit_ShouldReturnCorrectCount()
        {
            // arrange
            var visit_0 = new MovieWarehouseVisit() {
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null),
                MovieRips = new List<MovieRip>() { new MovieRip(), new MovieRip() }
                };
            var visit_1 = new MovieWarehouseVisit() {
                VisitDateTime = DateTime.ParseExact("20220102", "yyyyMMdd", null),
                MovieRips = new List<MovieRip>() { new MovieRip(), new MovieRip(), new MovieRip(), new MovieRip() }
                };
            this._movieWarehouseVisitRepositoryMock
                .Setup(m => m.GetAll())
                .Returns(new MovieWarehouseVisit[] { visit_0, visit_1 });

            // act
            Dictionary<DateTime, int> ripCountByVisit = this._scanRipsManager.GetRipCountByVisit();

            // assert
            var expected = new Dictionary<DateTime, int>() {
                [DateTime.ParseExact("20220101", "yyyyMMdd", null)] = 2,
                [DateTime.ParseExact("20220102", "yyyyMMdd", null)] = 4,
            };
            ripCountByVisit.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetVisitDiff_WithTwoNullVisits_ShouldThrowArgumentNullException()
        {
            // arrange
            // nothing to do...

            // act
            // nothing to do...

            // assert
            this._scanRipsManager.Invoking(s => s.GetVisitDiff(null, null)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetVisitDiff_WithNullVisitRight_ShouldThrowArgumentNullException()
        {
            // arrange
            var visitLeft = new MovieWarehouseVisit();

            // act
            // nothing to do...

            // assert
            this._scanRipsManager.Invoking(s => s.GetVisitDiff(visitLeft, null)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetVisitDiff_WithVisitLeftMoreRecentThanVisitRigh_ShouldThrowArgumentException()
        {
            // arrange
            var visitLeft = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220102", "yyyyMMdd", null) };
            var visitRight = new MovieWarehouseVisit() { VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null) };

            // act
            // nothing to to...

            //assert
            this._scanRipsManager
                .Invoking(r => r.GetVisitDiff(visitLeft, visitRight))
                .Should()
                .Throw<ArgumentException>();
        }

        [Fact]
        public void GetVisitDiff_WithOnlyVisitRight_ShouldReturnEmptyEnumerableInRemovedKeyAndAllRipsInAddedKey()
        {
            // arrange
            var movieRips = new List<MovieRip>() {
                new MovieRip() {
                    FileName = "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                    },
                new MovieRip() {
                    FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                    },
                new MovieRip() {
                    FileName = "Papillon.1973.1080p.BluRay.X264-AMIABLE",
                    }
            };
            var visitRight = new MovieWarehouseVisit() {
                MovieRips = movieRips,
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
                };

            // act
            Dictionary<string, IEnumerable<string>> visitDiff = this._scanRipsManager.GetVisitDiff(null, visitRight);

            // assert
            using (new AssertionScope())
            {
                visitDiff["removed"].Should().BeEmpty();
                visitDiff["added"].Should().BeEquivalentTo(movieRips.Select(r => r.FileName));
            }
        }

        [Fact]
        public void GetVisitDiff_WithTwoVisits_ShouldReturnCorrectDifference()
        {
            // arrange
            var movieRipsFirstVisit = new List<MovieRip>() {
                new MovieRip() {
                    FileName = "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                    },
                new MovieRip() {
                    FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                    }
            };
            var movieRipsSecondVisit = new List<MovieRip>() {
                new MovieRip() {
                    FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                    },
                new MovieRip() {
                    FileName = "Papillon.1973.1080p.BluRay.X264-AMIABLE",
                    }
            };
            DateTime firstVisitDate = DateTime.ParseExact("20220101", "yyyyMMdd", null);
            DateTime secondVisitDate = DateTime.ParseExact("20220102", "yyyyMMdd", null);

            var visitLeft = new MovieWarehouseVisit() {
                MovieRips = movieRipsFirstVisit,
                VisitDateTime = firstVisitDate
                };
            var visitRight = new MovieWarehouseVisit() {
                MovieRips = movieRipsSecondVisit,
                VisitDateTime = secondVisitDate
            };

            // act
            Dictionary<string, IEnumerable<string>> visitDiff = this._scanRipsManager.GetVisitDiff(visitLeft, visitRight);

            // assert
            using (new AssertionScope())
            {
                visitDiff["removed"].Should().BeEquivalentTo(new string[] { "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]" });
                visitDiff["added"].Should().BeEquivalentTo(new string[] { "Papillon.1973.1080p.BluRay.X264-AMIABLE" });
            }
        }

        [Fact]
        public void GetLastVisitDiff_ShouldReturnCorrectDifference()
        {
            // arrange
            var movieRip0 = new MovieRip() {
                FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
            };
            var movieRip1 = new MovieRip() {
                FileName = "Papillon.1973.1080p.BluRay.X264-AMIABLE",
            };
            var movieRip2 = new MovieRip() {
                FileName = "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
            };
            var movieRip3 = new MovieRip() {
                FileName = "Badlands.1973.1080p.BluRay.X264-AMIABLE",
            };

            var firstVisit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { movieRip0, movieRip1 },
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
            };
            var secondVisit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { movieRip1, movieRip2 },
                VisitDateTime = DateTime.ParseExact("20220102", "yyyyMMdd", null)
            };
            var thirdVisit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() { movieRip2, movieRip3 },
                VisitDateTime = DateTime.ParseExact("20220103", "yyyyMMdd", null)
            };

            this._movieWarehouseVisitRepositoryMock
                .Setup(m => m.GetClosestMovieWarehouseVisit())
                .Returns(thirdVisit);
            this._movieWarehouseVisitRepositoryMock
                .Setup(m => m.GetPreviousMovieWarehouseVisit(
                    It.Is<MovieWarehouseVisit>(m => m.VisitDateTime == thirdVisit.VisitDateTime)))
                .Returns(secondVisit);

            // act
            var lastVisitDiff = this._scanRipsManager.GetLastVisitDiff();

            // assert
            using (new AssertionScope())
            {
                lastVisitDiff["removed"].Should().BeEquivalentTo(new string[] { movieRip1.FileName });
                lastVisitDiff["added"].Should().BeEquivalentTo(new string[] { movieRip3.FileName });
            }
        }


        [Theory]
        [InlineData("gummo", 0)]
        [InlineData("gummo 1997", 0)]
        [InlineData("gummo.1997", 0)]
        [InlineData("gummo (1997)", 0)]
        [InlineData("Gummo.1997.DVDRip.XviD-DiSSOLVE", 0)]
        [InlineData("2160p", 1, 2)]
        [InlineData("the godfather", 1, 2)]
        [InlineData("the godfather part 2", 1, 2)]
        [InlineData("the GoDdfatheR   part ii  ", 1, 2)]
        [InlineData("1972", 1)]
        [InlineData("1080p")]
        [InlineData("dissolve", 0)]
        [InlineData("  petriFIED", 1, 2)]
        [InlineData("     ")]
        [InlineData(".")]
        public void SearchFromFileNameTokens_ShouldReturnExpectedEntities(string fileNameSearch, params int[] idsForExpectedResult)
        {
            // arrange
            var movieRip0 = new MovieRip()
            {
                Id = 0, FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE"
            };
            var movieRip1 = new MovieRip()
            {
                Id = 1, FileName = "The.Godfather.1972.2160p.WEB.H265-PETRiFiED"
            };
            var movieRip2 = new MovieRip()
            {
                Id = 2, FileName = "The.Godfather.Part.II.1974.2160p.WEB.H265-PETRiFiED"
            };

            var visit = new MovieWarehouseVisit()
            {
                MovieRips = new List<MovieRip>() { movieRip0, movieRip1, movieRip2 },
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
            };
            this._movieRipRepositoryMock
                .Setup(m => m.GetAllRipsInVisit(It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime)))
                .Returns(visit.MovieRips);

            // act
            IEnumerable<MovieRip> actualSearchResult = this._scanRipsManager.SearchFromFileNameTokens(visit, fileNameSearch);

            // assert
            actualSearchResult.Should().BeEquivalentTo(
                visit.MovieRips.Where(mr => idsForExpectedResult.Contains(mr.Id)));
        }


        [Theory]
        [InlineData("iSOMORFiSMO", new[] { 0, 1 })]
        [InlineData(" isomorfismo  ", new[] { 0, 1 })]
        [InlineData("isomorfismo[rarbg]", new[] { 0, 1 })]
        [InlineData("FGT", new[] { 2, 3 })]
        [InlineData("FGT[EtHD]", new[] { 2, 3 })]
        [InlineData("FGT[rarbg]", new[] { 2, 3 })]
        [InlineData("psychd", new[] { 4 })]
        [InlineData("CiNEFiLE", new int[] { })]
        public void GetRipsWithRipGroup_ShouldReturnExpectedEntities(string ripGroup, int[] expectedIds)
        {
            // arrange
            var movieRip0 = new MovieRip()
            {
                Id = 0,
                FileName = "Terra.de.Abril.1977.DVDRip.x264-iSOMORFiSMO",
                ParsedRipGroup = "iSOMORFiSMO"
            };
            var movieRip1 = new MovieRip()
            {
                Id = 1,
                FileName = "Pedras.da.Saudade.1989.DVDRip.x264-iSOMORFiSMO",
                ParsedRipGroup = "iSOMORFiSMO"
            };
            var movieRip2 = new MovieRip()
            {
                Id = 2,
                FileName = "Possum.2018.1080p.WEB-DL.DD5.1.H264-FGT[EtHD]",
                ParsedRipGroup = "FGT[EtHD]"
            };
            var movieRip3 = new MovieRip()
            {
                Id = 3,
                FileName = "Stroszek.1977.GERMAN.1080p.BluRay.x264.DTS-FGT",
                ParsedRipGroup = "FGT"
            };
            var movieRip4 = new MovieRip()
            {
                Id = 4,
                FileName = "Cobra.Verde.1987.GERMAN.1080p.BluRay.x264.PSYCHD",
                ParsedRipGroup = "PSYCHD"
            };
            var movieRip5 = new MovieRip()
            {
                Id = 5,
                FileName = "Taxidermia.2006",
                ParsedRipGroup = null
            };

            var visit = new MovieWarehouseVisit()
            {
                MovieRips = new List<MovieRip>() { movieRip0, movieRip1, movieRip2, movieRip3, movieRip4, movieRip5 },
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
            };
            this._movieRipRepositoryMock
                .Setup(m => m.GetAllRipsInVisit((It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime))))
                .Returns(visit.MovieRips);

            // act
            IEnumerable<MovieRip> actual = this._scanRipsManager.GetRipsWithRipGroup(visit, ripGroup);

            // assert
            IEnumerable<MovieRip> expected = visit.MovieRips.Where(mr => expectedIds.Contains(mr.Id));
            actual.Should().BeEquivalentTo(expected);
        }


        [Fact]
        public void GetRipCountByRipGroup_ShouldReturnCorrectCount()
        {
            // arrange
            var movieRip0 = new MovieRip()
            {
                Id = 0,
                FileName = "Terra.de.Abril.1977.DVDRip.x264-iSOMORFiSMO",
                ParsedRipGroup = "iSOMORFiSMO"
            };
            var movieRip1 = new MovieRip()
            {
                Id = 1,
                FileName = "Pedras.da.Saudade.1989.DVDRip.x264-iSOMORFiSMO",
                ParsedRipGroup = "iSOMORFiSMO"
            };
            var movieRip2 = new MovieRip()
            {
                Id = 2,
                FileName = "Possum.2018.1080p.WEB-DL.DD5.1.H264-FGT[EtHD]",
                ParsedRipGroup = "FGT[EtHD]"
            };
            var movieRip3 = new MovieRip()
            {
                Id = 3,
                FileName = "Stroszek.1977.GERMAN.1080p.BluRay.x264.DTS-FGT",
                ParsedRipGroup = "FGT"
            };
            var movieRip4 = new MovieRip()
            {
                Id = 4,
                FileName = "Cobra.Verde.1987.GERMAN.1080p.BluRay.x264.PSYCHD",
                ParsedRipGroup = "PSYCHD"
            };
            var movieRip5 = new MovieRip()
            {
                Id = 5,
                FileName = "Taxidermia.2006",
                ParsedRipGroup = null
            };

            var visit = new MovieWarehouseVisit()
            {
                MovieRips = new List<MovieRip>() { movieRip0, movieRip1, movieRip2, movieRip3, movieRip4, movieRip5 },
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
            };
            this._movieRipRepositoryMock
                .Setup(m => m.GetAllRipsInVisit((It.Is<MovieWarehouseVisit>(v => v.VisitDateTime == visit.VisitDateTime))))
                .Returns(visit.MovieRips);

            // act
            IEnumerable<KeyValuePair<string, int>> actual = this._scanRipsManager.GetRipCountByRipGroup(visit);

            // assert
            var expected = new KeyValuePair<string, int>[]
            {
                new KeyValuePair<string, int>("FGT", 2),
                new KeyValuePair<string, int>("iSOMORFiSMO", 2),
                new KeyValuePair<string, int>("PSYCHD", 1),
                new KeyValuePair<string, int>("<empty>", 1)
            };
            actual.Should().BeEquivalentTo(expected);
        }

    }
}
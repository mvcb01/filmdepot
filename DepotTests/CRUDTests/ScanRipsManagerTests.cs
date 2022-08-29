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
        public void GetRipCountByVisit_ReturnsCorrectCount()
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
    }
}

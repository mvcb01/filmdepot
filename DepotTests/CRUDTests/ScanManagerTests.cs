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

namespace DepotTests.CRUDTests
{
    public class ScanManagerTests
    {
        private readonly Mock<IMovieRipRepository> _movieRipRepositoryMock;
        private readonly  Mock<IMovieWarehouseVisitRepository> _movieWarehouseVisitRepositoryMock;
        private readonly  Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly ScanManager _scanManager;
        public ScanManagerTests()
        {
            this._movieRipRepositoryMock = new Mock<IMovieRipRepository>();
            this._movieWarehouseVisitRepositoryMock = new Mock<IMovieWarehouseVisitRepository>();

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.MovieWarehouseVisits)
                .Returns(this._movieWarehouseVisitRepositoryMock.Object);

            this._unitOfWorkMock
                .SetupGet(u => u.MovieRips)
                .Returns(this._movieRipRepositoryMock.Object);

            this._scanManager = new ScanManager(this._unitOfWorkMock.Object);
        }

        [Fact]
        public void GetRipCountByReleaseDate_ReturnsCorrectCount()
        {
            // arrange
            var latestVisit = new MovieWarehouseVisit() {
                MovieRips = new List<MovieRip>() {
                    new MovieRip() { ParsedReleaseDate = "1999" },
                    new MovieRip() { ParsedReleaseDate = "1999" },
                    new MovieRip() { ParsedReleaseDate = "2000" }
                }
            };
            this._movieWarehouseVisitRepositoryMock
                .Setup(m => m.GetClosestMovieWarehouseVisit())
                .Returns(latestVisit);


            // act
            var countByReleaseDate = this._scanManager.GetRipCountByReleaseDate();

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
            string releaseDate = "1997";
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

            this._movieWarehouseVisitRepositoryMock
                .Setup(v => v.GetClosestMovieWarehouseVisit())
                .Returns(new MovieWarehouseVisit() { MovieRips = movieRips });


            // act
            var ripsWithReleaseDate = this._scanManager.GetAllRipsWithReleaseDate(releaseDate);

            // assert
            var expected = new List<string>() {
                "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                "Gummo.1997.DVDRip.XviD-DiSSOLVE"
            };
            // da documentação:
            //     The two collections are equivalent when they both contain the same strings in any order.
            ripsWithReleaseDate.Should().BeEquivalentTo(expected);

            // garante que foi executado o método GetClosestMovieWarehouseVisit e não o IMovieRipRepository.Find
            _movieWarehouseVisitRepositoryMock.Verify(w => w.GetClosestMovieWarehouseVisit(), Times.Once);
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
            Dictionary<DateTime, int> ripCountByVisit = this._scanManager.GetRipCountByVisit();

            // assert
            var expected = new Dictionary<DateTime, int>() {
                [DateTime.ParseExact("20220101", "yyyyMMdd", null)] = 2,
                [DateTime.ParseExact("20220102", "yyyyMMdd", null)] = 4,
            };
            ripCountByVisit.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetLastVisitDiff_WithOnlyOneVisit_ShouldReturnEmptyEnumerableInRemovedKey()
        {
            // arrange
            var onlyVisit = new MovieWarehouseVisit() {
                MovieRips = new MovieRip[] { new MovieRip(), new MovieRip()},
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
                };
            this._movieWarehouseVisitRepositoryMock
                .Setup(w => w.GetClosestMovieWarehouseVisit())
                .Returns(onlyVisit);

            // act
            Dictionary<string, IEnumerable<string>> lastVisitDiff = this._scanManager.GetLastVisitDiff();

            // assert
            lastVisitDiff["removed"].Should().BeEmpty();
        }

        [Fact]
        public void GetLastVisitDiff_WithOnlyOneVisit_ShouldReturnAllRipsInAddedKey()
        {
            // arrange
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
            var onlyVisit = new MovieWarehouseVisit() {
                MovieRips = movieRips,
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
                };
            this._movieWarehouseVisitRepositoryMock
                .Setup(w => w.GetClosestMovieWarehouseVisit())
                .Returns(onlyVisit);

            // act
            Dictionary<string, IEnumerable<string>> lastVisitDiff = this._scanManager.GetLastVisitDiff();

            // assert
            lastVisitDiff["added"].Should().BeEquivalentTo(movieRips.Select(r => r.FileName));
        }

        [Fact]
        public void GetLastVisitDiff_WithTwoVisits_ShouldReturnCorrectDifference()
        {
            // arrange
            var movieRipsFirstVisit = new List<MovieRip>() {
                    new MovieRip() {
                        FileName = "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]",
                        ParsedReleaseDate = "1997"
                        },
                     new MovieRip() {
                        FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                        ParsedReleaseDate = "1997"
                        }
                };
            var movieRipsSecondVisit = new List<MovieRip>() {
                     new MovieRip() {
                        FileName = "Gummo.1997.DVDRip.XviD-DiSSOLVE",
                        ParsedReleaseDate = "1997"
                        },
                    new MovieRip() {
                        FileName = "Papillon.1973.1080p.BluRay.X264-AMIABLE",
                        ParsedReleaseDate = "1973"
                        }
                };
            var firstVisit = new MovieWarehouseVisit() {
                MovieRips = movieRipsFirstVisit,
                VisitDateTime = DateTime.ParseExact("20220101", "yyyyMMdd", null)
                };
            var secondVisit = new MovieWarehouseVisit() {
                MovieRips = movieRipsSecondVisit,
                VisitDateTime = DateTime.ParseExact("20220102", "yyyyMMdd", null)
            };
            this._movieWarehouseVisitRepositoryMock
                .Setup(w => w.GetAll())
                .Returns(new MovieWarehouseVisit[] { secondVisit, firstVisit });

            // act
            Dictionary<string, IEnumerable<string>> lastVisitDiff = this._scanManager.GetLastVisitDiff();

            // assert
            lastVisitDiff["removed"].Should().BeEquivalentTo(new string[] { "Face.Off.1997.iNTERNAL.1080p.BluRay.x264-MARS[rarbg]" });
            lastVisitDiff["added"].Should().BeEquivalentTo(new string[] { "Papillon.1973.1080p.BluRay.X264-AMIABLE" });
        }


    }
}
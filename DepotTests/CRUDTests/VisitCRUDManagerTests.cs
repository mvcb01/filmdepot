using System.IO;
using Xunit;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using FluentAssertions.Execution;
using System.Linq;

using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using FilmCRUD.Interfaces;
using ConfigUtils.Interfaces;
using FilmCRUD;
using FilmCRUD.CustomExceptions;


namespace DepotTests.CRUDTests
{
    public class VisitCRUDManagerTests
    {
        private readonly Mock<IFileSystemIOWrapper> _fileSystemIOWrapperMock;

        private readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

        private readonly Mock<IMovieWarehouseVisitRepository> _movieWarehouseVisitRepositoryMock;

        private readonly Mock<IMovieRipRepository> _movieRipRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly VisitCRUDManager _visitCRUDManager;


        public VisitCRUDManagerTests()
        {
            this._movieWarehouseVisitRepositoryMock = new Mock<IMovieWarehouseVisitRepository>(MockBehavior.Strict);
            this._movieRipRepositoryMock = new Mock<IMovieRipRepository>();

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.MovieWarehouseVisits)
                .Returns(this._movieWarehouseVisitRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.MovieRips)
                .Returns(this._movieRipRepositoryMock.Object);

            this._fileSystemIOWrapperMock = new Mock<IFileSystemIOWrapper>();
            this._appSettingsManagerMock = new Mock<IAppSettingsManager>();
            this._visitCRUDManager = new VisitCRUDManager(
                this._unitOfWorkMock.Object,
                this._fileSystemIOWrapperMock.Object,
                this._appSettingsManagerMock.Object);
        }

        [Fact]
        public void WriteMovieWarehouseContentsToTextFile_WithInexistantMovieWarehousePath_ShouldThrowDirectoryNotFoundException()
        {
            // arrange
            string inexistentMovieWarehousePath = "Z:\\Some\\Inexistent\\Dir";
            this._appSettingsManagerMock
                .Setup(a => a.GetMovieWarehouseDirectory())
                .Returns(inexistentMovieWarehousePath);
            this._fileSystemIOWrapperMock
                .Setup(f => f.DirectoryExists(inexistentMovieWarehousePath))
                .Returns(false);

            // act
            // nothing to do...

            // assert
            this._visitCRUDManager
                .Invoking(v => v.WriteMovieWarehouseContentsToTextFile())
                .Should()
                .Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void ReadWarehouseContentsAndRegisterVisit_WithExistingMovieWarehouseVisit_ShouldThrowDoubleVisitError()
        {
            // arrange
            string fileDateString = "20220101";
            this._movieWarehouseVisitRepositoryMock
                .Setup(v => v.GetVisitDates())
                .Returns(new DateTime[] { DateTime.ParseExact(fileDateString, "yyyyMMdd", null) });

            // act
            // nothing to do...

            // assert
            this._visitCRUDManager
                .Invoking(v => v.ReadWarehouseContentsAndRegisterVisit(fileDateString, failOnParsingErrors: false))
                .Should()
                .Throw<DoubleVisitError>();
        }

        [Fact]
        public void ReadWarehouseContentsAndRegisterVisit_WithInexistentTextFilesDirectory_ShouldThrowDirectoryNotFoundException()
        {
            // arrange
            // will always return false in bool methods
            this._fileSystemIOWrapperMock.SetReturnsDefault<bool>(false);

            this._movieWarehouseVisitRepositoryMock
                .Setup(v => v.GetVisitDates())
                .Returns(Enumerable.Empty<DateTime>());

            // act
            // nothing to do...

            // assert
            _visitCRUDManager
                .Invoking(v => v.ReadWarehouseContentsAndRegisterVisit("20220101"))
                .Should()
                .Throw<DirectoryNotFoundException>();
        }

        [Fact]
        public void ReadWarehouseContentsAndRegisterVisit_WithInexistentWarehouseContentsFile_ShouldThrowFileNotFoundException()
        {
            // arrange
            string textFilesPath = "S:\\Some\\TextFiles\\Directory";
            this._appSettingsManagerMock
                .Setup(a => a.GetWarehouseContentsTextFilesDirectory())
                .Returns(textFilesPath);
            this._fileSystemIOWrapperMock
                .Setup(f => f.DirectoryExists(It.IsAny<string>()))
                .Returns(true);
            this._fileSystemIOWrapperMock
                .Setup(f => f.GetFiles(textFilesPath))
                .Returns(new string[] { Path.Combine(textFilesPath, "movies_20220102.txt") });
            this._movieWarehouseVisitRepositoryMock
                .Setup(v => v.GetVisitDates())
                .Returns(Enumerable.Empty<DateTime>());

            // act
            // nothing to do...

            // assert
            this._visitCRUDManager
                .Invoking(v => v.ReadWarehouseContentsAndRegisterVisit("20220101", failOnParsingErrors: false))
                .Should()
                .Throw<FileNotFoundException>();
        }

        [Fact]
        public void GetMovieRipFileNamesInVisit_WithFilesToIgnore_ShouldNotReturnThem()
        {
            // arrange
            string[] filesToIgnore = { "The League Of Gentlemen", "Chernobyl.S01.1080p.AMZN.WEBRip.DDP5.1.x264-NTb[rartv]" };
            string[] textFileLines = {
                "The League Of Gentlemen",
                "Chernobyl.S01.1080p.AMZN.WEBRip.DDP5.1.x264-NTb[rartv]",
                "",
                "Sicario 2015 1080p BluRay x264 AC3-JYK",
                "      ",
                "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]"
            };
            _appSettingsManagerMock
                .Setup(a => a.GetFilesToIgnore())
                .Returns(filesToIgnore);
            _fileSystemIOWrapperMock
                .Setup(f => f.ReadAllLines(It.IsAny<string>()))
                .Returns(textFileLines);

            // act
            var fileNamesInVisit = _visitCRUDManager.GetMovieRipFileNamesInVisit("F:\\filepath\\does\\not\\matter.txt");

            // assert
            string[] expected = {
                "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                "Sicario 2015 1080p BluRay x264 AC3-JYK"
            };
            fileNamesInVisit.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void GetMovieRipsInVisit_WithoutManualInfo_ShouldReturnExpectedMovieRips()
        {
            // arrange
            string[] ripFileNamesInVisit = {
                "Sicario 2015 1080p BluRay x264 AC3-JYK",
                "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]",
                "The.Deer.Hunter.1978.REMASTERED.1080p.BluRay.x264.DTS-HD.MA.5.1-FGT"
            };
            MovieRip[] movieRipsInRepo = {
                new MovieRip() { FileName = "The.Deer.Hunter.1978.REMASTERED.1080p.BluRay.x264.DTS-HD.MA.5.1-FGT" },
                new MovieRip() { FileName = "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]" },
                new MovieRip() { FileName = "My.Cousin.Vinny.1992.1080p.BluRay.H264.AAC-RARBG" },
            };
            _movieRipRepositoryMock.Setup(m => m.GetAll()).Returns(movieRipsInRepo);
            
            // no manual info
            _appSettingsManagerMock
                .Setup(a => a.GetManualMovieRips())
                .Returns(new Dictionary<string, Dictionary<string, string>>());

            // act
            var (
                oldMovieRips,
                newMovieRips,
                newMovieRipsManual,
                allParsingErrors) = _visitCRUDManager.GetMovieRipsInVisit(ripFileNamesInVisit);

            // assert
            using (new AssertionScope())
            {
                oldMovieRips.GetFileNames().Should().BeEquivalentTo(new string[] {
                "The.Deer.Hunter.1978.REMASTERED.1080p.BluRay.x264.DTS-HD.MA.5.1-FGT",
                "Khrustalyov.My.Car.1998.720p.BluRay.x264-GHOULS[rarbg]"
                });

                newMovieRips.GetFileNames().Should().BeEquivalentTo(new string[] {
                    "Sicario 2015 1080p BluRay x264 AC3-JYK"
                });
                
                newMovieRipsManual.Should().BeEmpty();
                
                allParsingErrors.Should().BeEmpty();
            }
        }

        [Fact]
        public void GetMovieRipsInVisit_WithManualInfo_ShouldReturnExpectedMovieRips()
        {
            // arrange
            string[] ripFileNamesInVisit = {
                "2011 - some movie name - 720p",
                "The.Deer.Hunter.1978.REMASTERED.1080p.BluRay.x264.DTS-HD.MA.5.1-FGT"
            };
            MovieRip[] movieRipsInRepo = {
                new MovieRip() { FileName = "The.Deer.Hunter.1978.REMASTERED.1080p.BluRay.x264.DTS-HD.MA.5.1-FGT" }
            };
            var manualMovieRips = new Dictionary<string, Dictionary<string, string>>() {
                ["2011 - some movie name - 720p"] = new Dictionary<string, string>() {
                    ["FileName"] = "2011 - some movie name - 720p",
                    ["ParsedTitle"] = "some movie name"
                }
            };
            _movieRipRepositoryMock
                .Setup(m => m.GetAll())
                .Returns(movieRipsInRepo);
            _appSettingsManagerMock
                .Setup(a => a.GetManualMovieRips())
                .Returns(manualMovieRips);

            // act
            var (
                oldMovieRips,
                newMovieRips,
                newMovieRipsManual,
                allParsingErrors) = _visitCRUDManager.GetMovieRipsInVisit(ripFileNamesInVisit);

            // assert
            using (new AssertionScope())
            {
                oldMovieRips.GetFileNames().Should().BeEquivalentTo(new string[] {
                    "The.Deer.Hunter.1978.REMASTERED.1080p.BluRay.x264.DTS-HD.MA.5.1-FGT"
                });

                newMovieRips.Should().HaveCount(0);

                newMovieRipsManual.GetFileNames().Should().BeEquivalentTo(new string[] {
                    "2011 - some movie name - 720p"
                });

                allParsingErrors.Should().BeEmpty();
            }
        }


        [Fact]
        public void ProcessManuallyProvidedMovieRipsForExistingVisit_WithoutExistingMovieWarehouseVisit_ShouldThrowArgumentException()
        {
            // arrange
            this._movieWarehouseVisitRepositoryMock
                .Setup(v => v.GetVisitDates())
                .Returns(Enumerable.Empty<DateTime>());


            // act
            // nothing to do...

            // assert
            this._visitCRUDManager
                .Invoking(v => v.ProcessManuallyProvidedMovieRipsForExistingVisit("20220901"))
                .Should()
                .Throw<ArgumentException>();
        }

    }
}
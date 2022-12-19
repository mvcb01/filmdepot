using System.IO;
using Moq;
using FluentAssertions;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using FilmCRUD.Helpers;
using FilmCRUD.Interfaces;
using FilmCRUD.CustomExceptions;

namespace DepotTests.CRUDTests
{
    public class WarehouseListerTests
    {
        private readonly Mock<IFileSystemIOWrapper> _fileSystemIOWrapper;

        private readonly WarehouseLister _warehouseLister;

        public WarehouseListerTests()
        {
            this._fileSystemIOWrapper = new Mock<IFileSystemIOWrapper>(MockBehavior.Strict);
            this._warehouseLister = new WarehouseLister(this._fileSystemIOWrapper.Object);
        }

        [Fact]
        public void ListAndPersist_WithInexistentMovieWarehousePath_ShouldThrowDirectoryNotFoundException()
        {
            // arrange
            string inexistentWarehousePath = "Z:\\SomeDir";
            string existentDestinationDir = "D:\\DoesNotMatter";
            this._fileSystemIOWrapper
                .Setup(f => f.DirectoryExists(existentDestinationDir))
                .Returns(true);
            this._fileSystemIOWrapper
                .Setup(f => f.DirectoryExists(inexistentWarehousePath))
                .Returns(false);

            // act
            // nothing to do...

            // assert
            this._warehouseLister
                .Invoking(d => d.ListAndPersist(inexistentWarehousePath, existentDestinationDir, "movies_20220101.txt"))
                .Should()
                .Throw<DirectoryNotFoundException>()
                .WithMessage(inexistentWarehousePath);
        }

        [Fact]
        public void ListAndPersist_WithInexistentDestinationDirectory_ShouldThrowDirectoryNotFoundException()
        {
            // arrange
            string existentWarehousePath = "Z:\\WarehouseDir";
            string inexistentDestinationDirectory = "S:\\SomeDstDir";
            this._fileSystemIOWrapper.Setup(f => f.DirectoryExists(existentWarehousePath)).Returns(true);
            this._fileSystemIOWrapper.Setup(f => f.DirectoryExists(inexistentDestinationDirectory)).Returns(false);

            // act
            // nothing to do...

            // assert
            this._warehouseLister
                .Invoking(d => d.ListAndPersist(existentWarehousePath, inexistentDestinationDirectory, "movies_20220101.txt"))
                .Should()
                .Throw<DirectoryNotFoundException>()
                .WithMessage(inexistentDestinationDirectory);
        }

        [Fact]
        public void ListAndPersist_WithExistentFileName_ShouldThrowFileExistsError()
        {
            // arrange
            string existentWarehousePath = "Z:\\WarehouseDir";
            string existentDestinationDirectory = "S:\\SomeDstDir";
            string existentFileName = "movies_20220101.txt";
            string existentFilePath = Path.Combine(existentDestinationDirectory, existentFileName);
            this._fileSystemIOWrapper.Setup(f => f.DirectoryExists(existentWarehousePath)).Returns(true);
            this._fileSystemIOWrapper.Setup(f => f.DirectoryExists(existentDestinationDirectory)).Returns(true);
            this._fileSystemIOWrapper
                .Setup(f => f.GetFiles(existentDestinationDirectory))
                .Returns(new string[] { existentFilePath });
            this._fileSystemIOWrapper
                .Setup(f => f.GetSubdirectories(existentWarehousePath))
                .Returns(Enumerable.Empty<string>());

            // act
            // nothing to do...

            // assert
            this._warehouseLister
                .Invoking(d => d.ListAndPersist(existentWarehousePath, existentDestinationDirectory, "movies_20220101.txt" ))
                .Should()
                .Throw<FileExistsError>()
                .WithMessage(existentFilePath);
        }

        [Fact]
        public void GetMovieFileNames_WithExistentMovieWarehousePath_ShouldReturnCorrectDirectoryContents()
        {
            // arrange
            string existentWarehousePath = "Z:\\WarehouseDir";
            string[] movieFileNames = {
                "The.Lives.of.Others.2006.GERMAN.REMASTERED.1080p.BluRay.x264.DTS-NOGRP",
                "Sicario 2015 1080p BluRay x264 AC3-JYK"
            };
            IEnumerable<string> warehouseContents = movieFileNames.Select(s => Path.Combine(existentWarehousePath, s));
            this._fileSystemIOWrapper.Setup(f => f.DirectoryExists(existentWarehousePath)).Returns(true);
            this._fileSystemIOWrapper.Setup(f => f.GetSubdirectories(existentWarehousePath)).Returns(warehouseContents);

            // act
            IEnumerable<string> result = this._warehouseLister.GetMovieFileNames(existentWarehousePath);

            // assert
            // from the official docs:
            //     The two collections are equivalent when they both contain the same strings in any order.
            result.Should().BeEquivalentTo(movieFileNames);
        }
    }
}
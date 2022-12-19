using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Interfaces;
using FilmDomain.Entities;

namespace FilmCRUD.Helpers
{
    /// <summary>
    /// Class with utility methods to find and list movie files in a given directory. These movie files are expected to be directories
    /// and will later be converted to <see cref="MovieRip"/> entities.
    /// </summary>
    public class DirectoryFileLister
    {
        /// <summary>
        /// Useful to have this field so that we can mock its behaviour in tests, all while avoiding IO
        /// </summary>
        private readonly IFileSystemIOWrapper _fileSystemIOWrapper;

        public DirectoryFileLister(IFileSystemIOWrapper fileSystemIOWrapper) => this._fileSystemIOWrapper = fileSystemIOWrapper;

        public void ListMoviesAndPersistToTextFile(string movieWarehousePath, string destinationDirectory, string filename)
        {
            if (!this._fileSystemIOWrapper.DirectoryExists(destinationDirectory))
                throw new DirectoryNotFoundException(destinationDirectory);

            List<string> movieFiles = GetMovieFileNames(movieWarehousePath);

            string fileContents = String.Join("\n", movieFiles);

            PersistFileNamesToTextFile(destinationDirectory, filename, fileContents);
        }

        public List<string> GetMovieFileNames(string movieWarehousePath)
        {
            if (!_fileSystemIOWrapper.DirectoryExists(movieWarehousePath)) throw new DirectoryNotFoundException(movieWarehousePath);

            // movie rips are considered to be directories, not strict files
            IEnumerable<string> allMoviePaths = this._fileSystemIOWrapper.GetSubdirectories(movieWarehousePath);
            List<string> allFileNames = allMoviePaths.Select(m => Path.GetFileName(m)).ToList();

            return allFileNames;
        }

        private void PersistFileNamesToTextFile(string destinationDirectory, string fileName, string fileContents)
        {
            if (!_fileSystemIOWrapper.DirectoryExists(destinationDirectory))
                throw new DirectoryNotFoundException(destinationDirectory);

            string filePath = Path.Combine(destinationDirectory, fileName);

            if (_fileSystemIOWrapper.GetFiles(destinationDirectory).Contains(filePath))
                throw new FileExistsError(filePath);

            _fileSystemIOWrapper.WriteAllText(filePath, fileContents);
        }
    }

}
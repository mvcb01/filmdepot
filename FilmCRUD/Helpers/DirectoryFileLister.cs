using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Interfaces;

namespace FilmCRUD.Helpers
{
    public class DirectoryFileLister
    {

        private IFileSystemIOWrapper _fileSystemIOWrapper { get; init; }

        public DirectoryFileLister(IFileSystemIOWrapper fileSystemIOWrapper)
        {
            this._fileSystemIOWrapper = fileSystemIOWrapper;
        }

        public void ListMoviesAndPersistToTextFile(string movieWarehousePath, string destinationDirectory)
        {
            List<string> movieFiles = GetMovieFileNames(movieWarehousePath);

            string fileContents = String.Join("\n", movieFiles);

            string dateToday = DateTime.Now.ToString("yyyyMMdd");

            string fileName = $"movies_{dateToday}.txt";

            PersistFileNamesToTextFile(destinationDirectory, fileName, fileContents);
        }

        public List<string> GetMovieFileNames(string movieWarehousePath)
        {

            if (!_fileSystemIOWrapper.DirectoryExists(movieWarehousePath))
            {
                throw new DirectoryNotFoundException(movieWarehousePath);
            }

            // movie rips are considered to be directories, not strict files
            IEnumerable<string> allMoviePaths = _fileSystemIOWrapper.GetSubdirectories(movieWarehousePath);
            List<string> allFileNames = allMoviePaths.Select(m => Path.GetFileName(m)).ToList();

            return allFileNames;
        }

        private void PersistFileNamesToTextFile(string destinationDirectory, string fileName, string fileContents)
        {
            if (!_fileSystemIOWrapper.DirectoryExists(destinationDirectory))
            {
                throw new DirectoryNotFoundException(destinationDirectory);
            }

            string filePath = Path.Combine(destinationDirectory, fileName);

            if (_fileSystemIOWrapper.GetFiles(destinationDirectory).Contains(filePath))
            {
                throw new FileExistsError(filePath);
            }

            _fileSystemIOWrapper.WriteAllText(filePath, fileContents);
        }
    }
}
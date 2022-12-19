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
    /// Class with utility methods to find and list subdirectories in a given directory. These subdirectories will later be filtered
    /// and converted to <see cref="MovieRip"/> entities.
    /// </summary>
    public class WarehouseLister
    {
        /// <summary>
        /// Useful to have this field so that we can mock its behaviour in tests, all while avoiding IO
        /// </summary>
        private readonly IFileSystemIOWrapper _fileSystemIOWrapper;

        public WarehouseLister(IFileSystemIOWrapper fileSystemIOWrapper) => this._fileSystemIOWrapper = fileSystemIOWrapper;

        /// <summary>
        /// Finds all the subdirectories in <paramref name="warehousePath"/> and writes their names - one per line - to the 
        /// provided directory <paramref name="destinationDirectory"/> with the provided name <paramref name="filename"/>.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="FileExistsError"></exception>
        public void ListAndPersist(string warehousePath, string destinationDirectory, string filename)
        {
            if (!this._fileSystemIOWrapper.DirectoryExists(destinationDirectory))
                throw new DirectoryNotFoundException(destinationDirectory);

            IEnumerable<string> movieFiles = GetMovieFileNames(warehousePath);

            string fileContents = String.Join("\n", movieFiles);

            if (!this._fileSystemIOWrapper.DirectoryExists(destinationDirectory))
                throw new DirectoryNotFoundException(destinationDirectory);

            string filePath = Path.Combine(destinationDirectory, filename);

            if (this._fileSystemIOWrapper.GetFiles(destinationDirectory).Contains(filePath))
                throw new FileExistsError(filePath);

            this._fileSystemIOWrapper.WriteAllText(filePath, fileContents);
        }

        public IEnumerable<string> GetMovieFileNames(string movieWarehousePath)
        {
            if (!this._fileSystemIOWrapper.DirectoryExists(movieWarehousePath))
                throw new DirectoryNotFoundException(movieWarehousePath);

            // movie rips are considered to be directories, not strict files
            IEnumerable<string> allSubdirs = this._fileSystemIOWrapper.GetSubdirectories(movieWarehousePath);
            return allSubdirs.Select(m => Path.GetFileName(m));
        }
    }
}
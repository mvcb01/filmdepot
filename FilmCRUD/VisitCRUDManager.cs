using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;
using Serilog;
using ConfigUtils.Interfaces;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using FilmDomain.Entities;
using FilmCRUD.Helpers;
using FilmCRUD.CustomExceptions;
using FilmCRUD.Interfaces;

namespace FilmCRUD
{

    public class VisitCRUDManager
    {
        private IUnitOfWork _unitOfWork { get; init; }

        private IFileSystemIOWrapper _fileSystemIOWrapper { get; init; }

        private IAppSettingsManager _appSettingsManager { get; init; }

        private DirectoryFileLister _directoryFileLister { get; init; }

        // to match filenames like "movies_20220321.txt"
        private const string _txtFileRegex = @"^movies_20([0-9]{2})(0|1)[1-9][0-3][0-9].txt$";

        private readonly ILogger _logger;

        public string MovieWarehouseDirectory { get => _appSettingsManager.GetMovieWarehouseDirectory(); }

        public string WarehouseContentsTextFilesDirectory { get => _appSettingsManager.GetWarehouseContentsTextFilesDirectory(); }

        public Dictionary<string, Dictionary<string, string>> ManualMovieRips { get => _appSettingsManager.GetManualMovieRips(); }

        public IEnumerable<string> FilesToIgnore { get => _appSettingsManager.GetFilesToIgnore(); }


        public VisitCRUDManager(
            IUnitOfWork unitOfWork,
            IFileSystemIOWrapper fileSystemIOWrapper,
            IAppSettingsManager appSettingsManager)
        {
            this._unitOfWork = unitOfWork;
            this._fileSystemIOWrapper = fileSystemIOWrapper;
            this._directoryFileLister = new DirectoryFileLister(this._fileSystemIOWrapper);
            this._appSettingsManager = appSettingsManager;
        }

        public VisitCRUDManager(
            IUnitOfWork unitOfWork,
            IFileSystemIOWrapper fileSystemIOWrapper,
            IAppSettingsManager appSettingsManager,
            ILogger logger) : this(unitOfWork, fileSystemIOWrapper, appSettingsManager)
        {
            this._logger = logger;
        }

        public void WriteMovieWarehouseContentsToTextFile()
        {
            if (!this._fileSystemIOWrapper.DirectoryExists(WarehouseContentsTextFilesDirectory))
            {
                throw new DirectoryNotFoundException(WarehouseContentsTextFilesDirectory);
            }

            System.Console.WriteLine($"\nWriting the warehouse contents to {WarehouseContentsTextFilesDirectory}");

            _directoryFileLister.ListMoviesAndPersistToTextFile(MovieWarehouseDirectory, WarehouseContentsTextFilesDirectory);
        }


        public void ReadWarehouseContentsAndRegisterVisit(string fileDateString, bool failOnParsingErrors = false)
        {
            Log.Information("Persisting: {d}", fileDateString);
            this._logger?.Information("special logger - persisting: {d}", fileDateString);

            DateTime visitDate = DateTime.ParseExact(fileDateString, "yyyyMMdd", null);
            if (this._unitOfWork.MovieWarehouseVisits.GetVisitDates().Contains(visitDate))
            {
                throw new DoubleVisitError($"There's already a MovieWarehouseVisit for date {visitDate}");
            }

            // text file with the movie rip filenames
            string filePath = GetWarehouseContentsFilePath(fileDateString);

            // discards filenames to ignore, empty lines etc...
            IEnumerable<string> ripFileNamesInVisit = GetMovieRipFileNamesInVisit(filePath);

            var (oldMovieRips, newMovieRips, newMovieRipsManual, parsingErrors) = GetMovieRipsInVisit(ripFileNamesInVisit);

            int errorCount = parsingErrors.Count();
            if (errorCount > 0)
            {
                string errorsFpath = Path.Combine(WarehouseContentsTextFilesDirectory, $"parsing_errors_{fileDateString}.txt");
                string toWrite = "\nparsing errors: \n" + string.Join("\n", parsingErrors);
                _fileSystemIOWrapper.WriteAllText(errorsFpath, toWrite);

                string _msg = $"Errors while parsing movie rip filenames : {errorCount}; details in {errorsFpath}";
                if (failOnParsingErrors)
                {
                    throw new FileNameParserError(_msg);
                }
                else
                {
                    System.Console.WriteLine(_msg);
                }
            }
            List<MovieRip> allMovieRipsInVisit = oldMovieRips.Concat(newMovieRips).Concat(newMovieRipsManual).ToList();

            string visitDateStr = visitDate.ToString("MMMM dd yyyy");
            System.Console.WriteLine($"MovieWarehouseVisit: {visitDateStr}");
            System.Console.WriteLine($"Total rip count: {allMovieRipsInVisit.Count()}");
            System.Console.WriteLine($"Pre existing rips: {oldMovieRips.Count()}");
            System.Console.WriteLine($"New rips without manual info: {newMovieRips.Count()}");
            System.Console.WriteLine($"New rips with manual info: {newMovieRipsManual.Count()}");

            // persisting
            _unitOfWork.MovieWarehouseVisits.Add(new MovieWarehouseVisit() {
                VisitDateTime = visitDate,
                MovieRips = allMovieRipsInVisit
                });
            _unitOfWork.MovieRips.AddRange(newMovieRips);
            _unitOfWork.Complete();
        }

        public (
            IEnumerable<MovieRip> OldMovieRips,
            IEnumerable<MovieRip> NewMovieRips,
            IEnumerable<MovieRip> NewMovieRipsManual,
            IEnumerable<string> AllParsingErrors) GetMovieRipsInVisit(IEnumerable<string> ripFileNamesInVisit)
        {
            // pre existing MovieRip entities
            IEnumerable<MovieRip> allMovieRipsInRepo = _unitOfWork.MovieRips.GetAll();

            // pre existing MovieRip entities in this visit
            IEnumerable<MovieRip> oldMovieRips = allMovieRipsInRepo.Where(m => ripFileNamesInVisit.Contains(m.FileName));

            IEnumerable<string> _allRipFileNamesInRepo = allMovieRipsInRepo.GetFileNames().Select(f => f.Trim());
            IEnumerable<string> _newRipFileNames = ripFileNamesInVisit.Where(f => !_allRipFileNamesInRepo.Contains(f));

            Dictionary<string, Dictionary<string, string>> manualMovieRips = this.ManualMovieRips;

            IEnumerable<string> newRipFileNamesWithManualInfo = _newRipFileNames.Where(
                r => manualMovieRips.ContainsKey(r)
                );
            IEnumerable<string> newRipFileNamesWithoutManualInfo = _newRipFileNames.Except(newRipFileNamesWithManualInfo).ToList();

            IEnumerable<MovieRip> newMovieRipsManual = GetManualMovieRipsFromDictionaries(
                manualMovieRips.Where(kvp => newRipFileNamesWithManualInfo.Contains(kvp.Key)),
                out List<string> manualParsingErrors
                );

            IEnumerable<MovieRip> newMovieRips = ConvertFileNamesToMovieRips(newRipFileNamesWithoutManualInfo, out List<string> parsingErrors);

            List<string> allParsingErrors = manualParsingErrors.Concat(parsingErrors).ToList();

            // tuple with all movie rips in visit
            return (oldMovieRips, newMovieRips, newMovieRipsManual, allParsingErrors);
        }


        public IEnumerable<string> GetMovieRipFileNamesInVisit(string filePath)
        {
            return _fileSystemIOWrapper.ReadAllLines(filePath)
                .Select(f => f.Trim())
                .Where(f => (!string.IsNullOrWhiteSpace(f)) & (!this.FilesToIgnore.Contains(f)));
        }

        public void ProcessManuallyProvidedMovieRipsForExistingVisit(string visitDateString)
        {
            DateTime visitDate = DateTime.ParseExact(visitDateString, "yyyyMMdd", null);
            if (!this._unitOfWork.MovieWarehouseVisits.GetVisitDates().Contains(visitDate))
            {
                string _dateStr = visitDate.ToString("MMMM dd yyyy");
                throw new ArgumentException($"There's no MovieWarehouseVisit for date {_dateStr}");
            }

            MovieWarehouseVisit visit = this._unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(visitDate);

            string filePath = GetWarehouseContentsFilePath(visitDateString);

            IEnumerable<string> fileNamesInVisit = GetMovieRipFileNamesInVisit(filePath);

            IEnumerable<MovieRip> manualMovieRips = GetManualMovieRipsFromDictionaries(
                this.ManualMovieRips.Where(kvp => fileNamesInVisit.Contains(kvp.Key)),
                out List<string> manualParsingErrors);

            if (manualParsingErrors.Any())
            {
                string errorsFpath = Path.Combine(WarehouseContentsTextFilesDirectory, $"parsing_errors_manual_{visitDateString}.txt");
                string toWrite = "\nparsing errors: \n" + string.Join("\n", manualParsingErrors);
                _fileSystemIOWrapper.WriteAllText(errorsFpath, toWrite);
            }

            IEnumerable<MovieRip> movieRipsInVisit = this._unitOfWork.MovieRips.GetAllRipsInVisit(visit);
            foreach (var movieRip in manualMovieRips)
            {
                try
                {
                    MovieRip existingMovieRip = movieRipsInVisit.Where(r => r.FileName == movieRip.FileName).FirstOrDefault();
                    if (existingMovieRip == null)
                    {
                        Console.WriteLine($"Adding: {movieRip.FileName}");
                        visit.MovieRips.Add(movieRip);
                    }
                    else
                    {
                        Console.WriteLine($"Updating: {existingMovieRip.FileName}");
                        existingMovieRip.ParsedTitle = movieRip.ParsedTitle;
                        existingMovieRip.ParsedReleaseDate = movieRip.ParsedReleaseDate;
                        existingMovieRip.ParsedRipQuality = movieRip.ParsedRipQuality;
                        existingMovieRip.ParsedRipInfo = movieRip.ParsedRipInfo;
                        existingMovieRip.ParsedRipGroup = movieRip.ParsedRipGroup;
                    }
                }
                catch (Exception)
                {
                    this._unitOfWork.Dispose();
                    throw;
                }
            }

            this._unitOfWork.Complete();
            }

        private static IEnumerable<MovieRip> GetManualMovieRipsFromDictionaries(
            IEnumerable<KeyValuePair<string, Dictionary<string, string>>> manualMovieRipDictionaries,
            out List<string> manualParsingErrors)
        {
            List<MovieRip> manualMovieRips = new();
            manualParsingErrors = new List<string>();

            foreach (var item in manualMovieRipDictionaries)
            {
                string ripName = item.Key;
                Dictionary<string, string> ripDict = item.Value;

                try
                {
                    string dictSerialized = JsonSerializer.Serialize(ripDict);
                    MovieRip rip = JsonSerializer.Deserialize<MovieRip>(dictSerialized);
                    manualMovieRips.Add(rip);
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is NotSupportedException || ex is JsonException)
                {
                    manualParsingErrors.Add(ripName);
                }
            }

            return manualMovieRips;
        }

        private static IEnumerable<MovieRip> ConvertFileNamesToMovieRips(IEnumerable<string> ripFileNames, out List<string> parsingErrors)
        {
            var movieRips = new List<MovieRip>();
            parsingErrors = new List<string>();
            foreach (var fileName in ripFileNames)
            {
                try
                {
                    MovieRip rip = FileNameParser.ParseFileNameIntoMovieRip(fileName);
                    movieRips.Add(rip);
                }
                catch (FileNameParserError)
                {
                    parsingErrors.Add(fileName);
                }
            }

            return movieRips;
        }

        private string GetWarehouseContentsFilePath(string fileDateString)
        {
            if (!this._fileSystemIOWrapper.DirectoryExists(WarehouseContentsTextFilesDirectory))
            {
                throw new DirectoryNotFoundException(WarehouseContentsTextFilesDirectory);
            }

            Predicate<string> _isFileMatch = f => Regex.Matches(f, _txtFileRegex).Count() == 1;

            IEnumerable<string> relevantFiles = _fileSystemIOWrapper
                .GetFiles(WarehouseContentsTextFilesDirectory)
                .Where(fPath => _isFileMatch(Path.GetFileName(fPath)));

            List<string> filesWithDate = relevantFiles.Where(f => f.EndsWith($"_{fileDateString}.txt")).ToList();

            if (!filesWithDate.Any())
            {
                throw new FileNotFoundException($"No warehouse contents files with suffix _{fileDateString}.txt");
            }
            else if (filesWithDate.Count > 1)
            {
                throw new FileNotFoundException($"Several warehouse contents files with suffix _{fileDateString}.txt");
            }

            return filesWithDate.First();
        }

    }
}
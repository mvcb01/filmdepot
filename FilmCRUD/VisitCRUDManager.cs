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

        private readonly ILogger _errorLogger;

        public string MovieWarehouseDirectory { get => _appSettingsManager.GetMovieWarehouseDirectory(); }

        public string WarehouseContentsTextFilesDirectory { get => _appSettingsManager.GetWarehouseContentsTextFilesDirectory(); }

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
            ILogger errorLogger) : this(unitOfWork, fileSystemIOWrapper, appSettingsManager) => this._errorLogger = errorLogger;


        public void WriteMovieWarehouseContentsToTextFile()
        {
            string filename = $"movies_{DateTime.Now:yyyyMMdd}.txt";

            Log.Information("Writing the warehouse contents to {DirPath}: {FileName}", WarehouseContentsTextFilesDirectory, filename);

            try
            {
                this._directoryFileLister.ListMoviesAndPersistToTextFile(MovieWarehouseDirectory, WarehouseContentsTextFilesDirectory, filename);
            }
            catch (Exception ex) when (ex is FileExistsError || ex is DirectoryNotFoundException)
            {
                Log.Error(ex, ex.Message);
                throw;
            }
            
        }


        public void ReadWarehouseContentsAndRegisterVisit(string fileDateString)
        {
            DateTime visitDate = DateTime.ParseExact(fileDateString, "yyyyMMdd", null);
            if (this._unitOfWork.MovieWarehouseVisits.GetVisitDates().Contains(visitDate))
            {
                Log.Error("There's already a visit for date {VisitDate}", visitDate.ToString("MMMM dd yyyy"));
                throw new DoubleVisitError(fileDateString);
            }

            // text file with the movie rip filenames
            string filePath = GetWarehouseContentsFilePath(fileDateString);

            // discards filenames to ignore, empty lines etc...
            IEnumerable<string> ripFileNamesInVisit = GetMovieRipFileNamesInVisit(filePath);

            Log.Information("Movie files in visit - total: {RipCount}", ripFileNamesInVisit.Count());

            var (oldMovieRips, newMovieRips, newMovieRipsManual, parsingErrors) = GetMovieRipsInVisit(ripFileNamesInVisit);

            int errorCount = parsingErrors.Count();
            if (errorCount > 0)
            {
                string errorsFpath = Path.Combine(WarehouseContentsTextFilesDirectory, $"parsing_errors_{fileDateString}.txt");
                string toWrite = "\nparsing errors: \n" + string.Join("\n", parsingErrors);
                _fileSystemIOWrapper.WriteAllText(errorsFpath, toWrite);

                string _msg = $"Errors while parsing movie rip filenames : {errorCount}; details in {errorsFpath}";
                System.Console.WriteLine(_msg);
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

            Log.Information("Movie files in visit - new: {NewRipCount}", _newRipFileNames.Count());

            Dictionary<string, Dictionary<string, string>> manualMovieRipsCfg = this._appSettingsManager.GetManualMovieRips();

            // calling .ToList to trigger loading
            IEnumerable<string> newRipFileNamesWithManualInfo = _newRipFileNames.Where(r => manualMovieRipsCfg.ContainsKey(r)).ToList();
            IEnumerable<string> newRipFileNamesWithoutManualInfo = _newRipFileNames.Except(newRipFileNamesWithManualInfo).ToList();

            IEnumerable<MovieRip> newMovieRipsManual = GetManualMovieRipsFromDictionaries(
                manualMovieRipsCfg.Where(kvp => newRipFileNamesWithManualInfo.Contains(kvp.Key)),
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
                .Where(f => (!string.IsNullOrWhiteSpace(f)) & (!this._appSettingsManager.GetFilesToIgnore().Contains(f)));
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

            Dictionary<string, Dictionary<string, string>> manualMovieRipsCfg = this._appSettingsManager.GetManualMovieRips();

            IEnumerable<MovieRip> manualMovieRips = GetManualMovieRipsFromDictionaries(
                manualMovieRipsCfg.Where(kvp => fileNamesInVisit.Contains(kvp.Key)),
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
            var manualMovieRips = new List<MovieRip>();
            manualParsingErrors = new List<string>();

            int totalCount = manualMovieRipDictionaries.Count();
            var logStep = (int)Math.Ceiling((decimal)totalCount / 20.0m);
            foreach (var (item, idx) in manualMovieRipDictionaries.Select((value, idx) => (value, idx + 1)))
            {
                string ripName = item.Key;
                Dictionary<string, string> ripDict = item.Value;

                try
                {
                    Log.Debug("Manual info for {RipName}", ripName);
                    string dictSerialized = JsonSerializer.Serialize(ripDict);
                    MovieRip rip = JsonSerializer.Deserialize<MovieRip>(dictSerialized);
                    manualMovieRips.Add(rip);
                }
                catch (Exception ex) when (ex is ArgumentNullException || ex is NotSupportedException || ex is JsonException)
                {
                    Log.Debug("---> ERROR: {RipName}", ripName);
                    manualParsingErrors.Add(ripName);
                }

                if (idx % logStep == 0 || idx == totalCount)
                {
                    Log.Information("Creating MovieRip entities from manual info: {Index} out of {Total}", idx, totalCount);
                }
            }

            return manualMovieRips;
        }

        private static IEnumerable<MovieRip> ConvertFileNamesToMovieRips(IEnumerable<string> ripFileNames, out List<string> parsingErrors)
        {
            var movieRips = new List<MovieRip>();
            parsingErrors = new List<string>();

            int totalCount = ripFileNames.Count();
            var logStep = (int)Math.Ceiling((decimal)totalCount / 20.0m);
            foreach (var (fileName, idx) in ripFileNames.Select((value, idx) => (value, idx + 1)))
            {
                Log.Debug("Parsing: {MovieRipFileName}", fileName);
                try
                {
                    MovieRip rip = FileNameParser.ParseFileNameIntoMovieRip(fileName);
                    movieRips.Add(rip);
                }
                catch (FileNameParserError)
                {
                    Log.Debug("---> PARSING ERROR: {MovieRipFileName}", fileName);
                    parsingErrors.Add(fileName);
                }

                if (idx % logStep == 0 || idx == totalCount)
                {
                    Log.Information("Parsing filenames into MovieRip entities: {Index} out of {Total}", idx, totalCount);
                }
            }

            return movieRips;
        }

        private string GetWarehouseContentsFilePath(string fileDateString)
        {
            if (!this._fileSystemIOWrapper.DirectoryExists(WarehouseContentsTextFilesDirectory))
            {
                Log.Error("Not a directory: {DirPath}", WarehouseContentsTextFilesDirectory);
                throw new DirectoryNotFoundException(WarehouseContentsTextFilesDirectory);
            }

            Predicate<string> _isFileMatch = f => Regex.Matches(f, _txtFileRegex).Count() == 1;

            IEnumerable<string> relevantFiles = _fileSystemIOWrapper
                .GetFiles(WarehouseContentsTextFilesDirectory)
                .Where(fPath => _isFileMatch(Path.GetFileName(fPath)));

            List<string> filesWithDate = relevantFiles.Where(f => f.EndsWith($"_{fileDateString}.txt")).ToList();

            if (!filesWithDate.Any())
            {
                Log.Error("No warehouse contents files with suffix _{FileDateString}.txt", fileDateString);
                throw new FileNotFoundException(fileDateString);
            }
            else if (filesWithDate.Count > 1)
            {
                Log.Error("Several warehouse contents files with suffix _{FileDateString}.txt", fileDateString);
                throw new FileNotFoundException(fileDateString);
            }

            return filesWithDate.First();
        }

    }
}
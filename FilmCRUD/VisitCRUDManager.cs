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
        private const string _txtFileRegex = @"^movies_20([0-9]{2})(0|1)[0-9][0-3][0-9].txt$";

        private readonly ILogger _parsingErrorsLogger;

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
            ILogger parsingErrorsLogger) : this(unitOfWork, fileSystemIOWrapper, appSettingsManager) => this._parsingErrorsLogger = parsingErrorsLogger;


        public void WriteMovieWarehouseContentsToTextFile()
        {
            string filename = $"movies_{DateTime.Now:yyyyMMdd}.txt";

            Log.Information("Writing the warehouse contents to {fpath}", Path.Combine(WarehouseContentsTextFilesDirectory, filename));

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
                this._parsingErrorsLogger?.Information("----------------------------------");
                this._parsingErrorsLogger?.Information("--- {DateTime} ---", DateTime.Now.ToString("g"));
                this._parsingErrorsLogger?.Information("----------------------------------");
                parsingErrors.ToList().ForEach(e => this._parsingErrorsLogger?.Error(e));
            }

            // calling .ToList since property MovieWarehouseVisit.MovieRips is a ICollection
            List<MovieRip> allMovieRipsInVisit = oldMovieRips.Concat(newMovieRips).Concat(newMovieRipsManual).ToList();

            Log.Information("------------ VISIT SUMMARY ------------");
            Log.Information("MovieWarehouseVisit: {VisitDate}", visitDate.ToString("MMMM dd yyyy"));
            Log.Information("Parsing errors count: {Count}", errorCount);
            Log.Information("Total parsed rips count: {TotalRipCount}", allMovieRipsInVisit.Count());
            Log.Information("Pre existing rips: {PreExistingRips}", oldMovieRips.Count());
            Log.Information("New rips without manual info: {NewRips}", newMovieRips.Count()); 
            Log.Information("New rips with manual info: {NewRipsManual}", newMovieRipsManual.Count());

            // persisting changes
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
            IEnumerable<MovieRip> allMovieRipsInRepo = this._unitOfWork.MovieRips.GetAll().ToList();

            // pre existing MovieRip entities in this visit
            // just a filter on all MovieRip entities, basically a left-semi join on the filenames
            IEnumerable<MovieRip> oldMovieRips = allMovieRipsInRepo
                .Join(
                    ripFileNamesInVisit,
                    mrip => mrip.FileName,
                    filename => filename,
                    (mrip, f) => mrip)
                .ToList();

            IEnumerable<string> _allRipFileNamesInRepo = allMovieRipsInRepo.GetFileNames().Select(f => f.Trim()).ToList();
            IEnumerable<string> _newRipFileNames = ripFileNamesInVisit.Where(f => !_allRipFileNamesInRepo.Contains(f));

            Log.Information("Movie files in visit - new: {NewRipCount}", _newRipFileNames.Count());

            Dictionary<string, Dictionary<string, string>> manualMovieRipsCfg = this._appSettingsManager.GetManualMovieRips();

            // calling .ToList to trigger loading
            IEnumerable<string> newRipFileNamesWithManualInfo = _newRipFileNames.Where(r => manualMovieRipsCfg.ContainsKey(r));
            IEnumerable<string> newRipFileNamesWithoutManualInfo = _newRipFileNames.Except(newRipFileNamesWithManualInfo);

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
            return this._fileSystemIOWrapper.ReadAllLines(filePath)
                .Select(f => f.Trim())
                .Where(f => (!string.IsNullOrWhiteSpace(f)) & (!this._appSettingsManager.GetFilesToIgnore().Contains(f)));
        }

        public void ProcessManuallyProvidedMovieRipsForExistingVisit(string visitDateString)
        {
            DateTime visitDate = DateTime.ParseExact(visitDateString, "yyyyMMdd", null);
            if (!this._unitOfWork.MovieWarehouseVisits.GetVisitDates().Contains(visitDate))
            {
                var dateStr = visitDate.ToString("MMMM dd yyyy");
                Log.Error("There's no MovieWarehouseVisit for date {VisitDate}", dateStr);
                throw new ArgumentException(dateStr);
            }

            // exact match is guaranteed
            MovieWarehouseVisit visit = this._unitOfWork.MovieWarehouseVisits.GetClosestMovieWarehouseVisit(visitDate);

            string filePath = GetWarehouseContentsFilePath(visitDateString);

            IEnumerable<string> fileNamesInVisit = GetMovieRipFileNamesInVisit(filePath);

            Dictionary<string, Dictionary<string, string>> manualMovieRipsCfg = this._appSettingsManager.GetManualMovieRips();

            IEnumerable<MovieRip> manualMovieRips = GetManualMovieRipsFromDictionaries(
                manualMovieRipsCfg.Where(kvp => fileNamesInVisit.Contains(kvp.Key)),
                out List<string> manualParsingErrors);

            int errorCount = manualParsingErrors.Count();
            if (errorCount > 0)
            {
                Log.Information("Parsing error count for manually configured movie files: {Count}", errorCount);
                this._parsingErrorsLogger?.Information("----------------------------------");
                this._parsingErrorsLogger?.Information("--- {DateTime} ---", DateTime.Now.ToString("g"));
                this._parsingErrorsLogger?.Information("----------------------------------");
                manualParsingErrors.ToList().ForEach(e => this._parsingErrorsLogger?.Error(e));
            }

            IEnumerable<MovieRip> allMovieRips = this._unitOfWork.MovieRips.GetAll();

            // caching the filenames
            IEnumerable<string> filenamesForMovieRipsInVisit = this._unitOfWork.MovieRips
                .GetAllRipsInVisit(visit)
                .Select(mr => mr.FileName)
                .ToArray();

            foreach (var movieRip in manualMovieRips)
            {
                try
                {
                    // updates the MovieRip entity if it already exists in the repo, otherwise points to the instance
                    // created from the manual configs
                    MovieRip targetMovieRip = allMovieRips.Where(r => r.FileName == movieRip.FileName).FirstOrDefault();
                    if (targetMovieRip != null)
                    {
                        // not too many files are expected to be manually configured so we use Information level
                        Log.Information("Updating movie rip: {FileName}", targetMovieRip.FileName);
                        targetMovieRip.ParsedTitle = movieRip.ParsedTitle;
                        targetMovieRip.ParsedReleaseDate = movieRip.ParsedReleaseDate;
                        targetMovieRip.ParsedRipQuality = movieRip.ParsedRipQuality;
                        targetMovieRip.ParsedRipInfo = movieRip.ParsedRipInfo;
                        targetMovieRip.ParsedRipGroup = movieRip.ParsedRipGroup;
                    }
                    else
                    {
                        targetMovieRip = movieRip;
                    }

                    // if the target MovieRip entity wasn't already part of the existing visit then it needs to be added;
                    // not too many files are expected to be manually configured so we use the log Information level
                    if (!filenamesForMovieRipsInVisit.Contains(targetMovieRip.FileName))
                    {

                        Log.Information("Adding movie rip to visit {VisitDateString}: {FileName}", visitDateString, targetMovieRip.FileName);
                        visit.MovieRips.Add(targetMovieRip);
                    }
                    else
                    {
                        Log.Information("Movie rip was already in visit {VisitDateString}: {FileName}", visitDateString, targetMovieRip.FileName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
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
                    Log.Debug("\t-> PARSING ERROR: {MovieRipFileName}", fileName);
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
                Log.Error("No warehouse contents files with suffix _{FileDateString}.txt; regex filter used: {Regex}", fileDateString, _txtFileRegex);
                throw new FileNotFoundException(fileDateString);
            }
            else if (filesWithDate.Count > 1)
            {
                Log.Error("Several warehouse contents files with suffix _{FileDateString}.txt; regex filter used: {Regex}", fileDateString, _txtFileRegex);
                throw new FileNotFoundException(fileDateString);
            }

            return filesWithDate.First();
        }

    }
}
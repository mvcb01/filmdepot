using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Text.Json;

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

        // para dar match a ficheiros da forma movies_20220321.txt
        private const string TxtFileRegex = @"^movies_20([0-9]{2})(0|1)[1-9][0-3][0-9].txt$";

        public string MovieWarehouseDirectory { get { return _appSettingsManager.GetMovieWarehouseDirectory(); }}

        public string WarehouseContentsTextFilesDirectory { get { return _appSettingsManager.GetWarehouseContentsTextFilesDirectory(); } }

        public Dictionary<string, Dictionary<string, string>> ManualMovieRips { get { return _appSettingsManager.GetManualMovieRips(); } }

        public IEnumerable<string> FilesToIgnore { get { return _appSettingsManager.GetFilesToIgnore(); } }


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

        public void WriteMovieWarehouseContentsToTextFile()
        {
            if (!_fileSystemIOWrapper.DirectoryExists(WarehouseContentsTextFilesDirectory))
            {
                throw new DirectoryNotFoundException(WarehouseContentsTextFilesDirectory);
            }

            System.Console.WriteLine($"\nA escrever os conteúdos da warehouse para {WarehouseContentsTextFilesDirectory}");

            _directoryFileLister.ListMoviesAndPersistToTextFile(MovieWarehouseDirectory, WarehouseContentsTextFilesDirectory);
        }


        public void ReadWarehouseContentsAndRegisterVisit(string fileDateString, bool failOnParsingErrors = false)
        {
            DateTime visitDate = DateTime.ParseExact(fileDateString, "yyyyMMdd", null);
            if (_unitOfWork.MovieWarehouseVisits.GetAll().GetVisitDates().Contains(visitDate))
            {
                throw new DoubleVisitError($"Já existe uma MovieWarehouseVisit na data {visitDate}");
            }

            // txt com os movie rips
            string filePath = GetWarehouseContentsFilePath(fileDateString);

            // todos os rip filenames que estão no txt e que não são para ignorar nem são strings vazias
            IEnumerable<string> ripFileNamesInVisit = GetMovieRipFileNamesInVisit(filePath);

            var (oldMovieRips, newMovieRips, newMovieRipsManual, parsingErrors) = GetMovieRipsInVisit(ripFileNamesInVisit);

            int errorCount = parsingErrors.Count();
            if (errorCount > 0)
            {
                string errorsFpath = Path.Combine(WarehouseContentsTextFilesDirectory, $"parsing_errors_{fileDateString}.txt");
                string toWrite = "\nparsing errors: \n" + string.Join("\n", parsingErrors);
                _fileSystemIOWrapper.WriteAllText(errorsFpath, toWrite);

                string _msg = $"Erros no parse de filenames para objectos MovieRip : {errorCount}; detalhes em {errorsFpath}";
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

            System.Console.WriteLine($"MovieWarehouseVisit: {fileDateString}");
            System.Console.WriteLine($"Total de movie rips na visita: {allMovieRipsInVisit.Count()}");
            System.Console.WriteLine($"Movie rips já existentes: {oldMovieRips.Count()}");
            System.Console.WriteLine($"Movie rips novos sem info manual: {newMovieRips.Count()}");
            System.Console.WriteLine($"Movie rips novos com info manual: {newMovieRipsManual.Count()}");

            // persistência
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
            // rips já existentes no repositório
            IEnumerable<MovieRip> allMovieRipsInRepo = _unitOfWork.MovieRips.GetAll();

            // MovieRips que já estavam no repo e que fazem parte desta visita
            IEnumerable<MovieRip> oldMovieRips = allMovieRipsInRepo.Where(m => ripFileNamesInVisit.Contains(m.FileName));

            IEnumerable<string> _allRipFileNamesInRepo = allMovieRipsInRepo.GetFileNames().Select(f => f.Trim());
            IEnumerable<string> _newRipFileNames = ripFileNamesInVisit.Where(f => !_allRipFileNamesInRepo.Contains(f));

            Dictionary<string, Dictionary<string, string>> manualMovieRips = this.ManualMovieRips;

            // rip filenames novos com info manual
            IEnumerable<string> newRipFileNamesWithManualInfo = _newRipFileNames.Where(
                r => manualMovieRips.Keys.Contains(r)
                );
            // rip filenames novos sem info manual
            IEnumerable<string> newRipFileNamesWithoutManualInfo = _newRipFileNames.Except(newRipFileNamesWithManualInfo).ToList();

            List<string> manualParsingErrors = new();
            IEnumerable<MovieRip> newMovieRipsManual = GetManualMovieRipsFromDictionaries(
                manualMovieRips.Where(kvp => newRipFileNamesWithManualInfo.Contains(kvp.Key)),
                out manualParsingErrors
                );

            List<string> parsingErrors = new();
            IEnumerable<MovieRip> newMovieRips = ConvertFileNamesToMovieRips(newRipFileNamesWithoutManualInfo, out parsingErrors);

            List<string> allParsingErrors = manualParsingErrors.Concat(parsingErrors).ToList();

            // todos os objects MovieRip desta visita
            return (oldMovieRips, newMovieRips, newMovieRipsManual, allParsingErrors);
        }


        public IEnumerable<string> GetMovieRipFileNamesInVisit(string filePath)
        {
            IEnumerable<string> filesToIgnore = this.FilesToIgnore;

            return _fileSystemIOWrapper.ReadAllLines(filePath)
                .Select(f => f.Trim())
                .Where(f => (!string.IsNullOrWhiteSpace(f)) & (!filesToIgnore.Contains(f)));
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
            List<MovieRip> movieRips = new();
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
            if (!_fileSystemIOWrapper.DirectoryExists(WarehouseContentsTextFilesDirectory))
            {
                throw new DirectoryNotFoundException(WarehouseContentsTextFilesDirectory);
            }

            Predicate<string> _isFileMatch = f => Regex.Matches(f, TxtFileRegex).Count() == 1;

            IEnumerable<string> relevantFiles = _fileSystemIOWrapper
                .GetFiles(WarehouseContentsTextFilesDirectory)
                .Where(fPath => _isFileMatch(Path.GetFileName(fPath)));

            List<string> filesWithDate = relevantFiles.Where(f => f.EndsWith($"_{fileDateString}.txt")).ToList();

            if (filesWithDate.Count() == 0)
            {
                throw new FileNotFoundException($"Não há ficheiros de warehouse contents com sufixo _{fileDateString}.txt");
            }
            else if (filesWithDate.Count() > 1)
            {
                throw new FileNotFoundException($"Vários ficheiros de warehouse contents com sufixo _{fileDateString}.txt");
            }

            return filesWithDate.First();
        }

    }
}
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using CommandLine;
using Serilog;
using Serilog.Events;
using System.Globalization;
using FilmDataAccess.EFCore.UnitOfWork;
using FilmDomain.Entities;
using FilmDomain.Interfaces;
using FilmDomain.Extensions;
using FilmDataAccess.EFCore;
using FilmCRUD.Verbs;
using FilmCRUD.Interfaces;
using FilmCRUD.Helpers;
using ConfigUtils;
using ConfigUtils.Interfaces;
using MovieAPIClients.Interfaces;
using MovieAPIClients.TheMovieDb;

namespace FilmCRUD
{
    class Program
    {
        // default serilog template without the timezone
        private const string _logOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        static async Task Main(string[] args)
        {
            // CONFIGURING MAIN LOGGER FROM THE Serilog.Log STATIC CLASS

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Information, outputTemplate: _logOutputTemplate)
                .WriteTo.File(
                    "logs/filmcrud_.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: _logOutputTemplate)
                .CreateLogger();

            try
            {
                // ------------------
                // DI

                ServiceCollection services = new();
                ConfigureServices(services);
                ServiceProvider serviceProvider = services.BuildServiceProvider();

                // ------------------
                // PARSING ARGS AND EXECUTING

                ParserResult<object> parsed = Parser
                    .Default
                    .ParseArguments<VisitOptions, ScanRipsOptions, ScanMoviesOptions, LinkOptions, FetchOptions>(args);

                parsed.WithParsed<VisitOptions>(opts => HandleVisitOptions(opts, serviceProvider));

                parsed.WithParsed<ScanRipsOptions>(opts => HandleScanRipsOptions(opts, serviceProvider));

                parsed.WithParsed<ScanMoviesOptions>(opts => HandleScanMoviesOptions(opts, serviceProvider));

                await parsed.WithParsedAsync<LinkOptions>(async opts => await HandleLinkOptions(opts, serviceProvider));

                await parsed.WithParsedAsync<FetchOptions>(async opts => await HandleFetchOptions(opts, serviceProvider));

                parsed.WithNotParsed(HandleParseError);
            }
            finally
            {
                // DISPOSING LOGGER
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IUnitOfWork, SQLiteUnitOfWork>(_ => new SQLiteUnitOfWork(new SQLiteAppContext()));

            services.AddSingleton<IFileSystemIOWrapper, FileSystemIOWrapper>();

            services.AddSingleton<IAppSettingsManager, AppSettingsManager>();

            services.AddSingleton<IMovieAPIClient, TheMovieDbAPIClient>();
        }

        private static void HandleVisitOptions(VisitOptions opts, ServiceProvider serviceProvider)
        {
            // local func to create the logger that saves parsing errors;
            // made static since it does not need local variables or instance state
            static ILogger GetLoggerForParsingErrors(string visitDateString)
            {
                bool validDate = DateTime.TryParseExact(visitDateString, "yyyyMMdd", null, DateTimeStyles.None, out _);
                if (!validDate)
                {
                    Log.Error("Should be a date with format yyyyMMdd: {VisitDateString}", visitDateString);
                    throw new FormatException(visitDateString);
                }

                // default MinimumLevel is Information
                return new LoggerConfiguration()
                    .WriteTo.File(
                        $"logs/parsing_errors_visit{visitDateString}.txt",
                        rollingInterval: RollingInterval.Infinite,
                        outputTemplate: _logOutputTemplate)
                    .CreateLogger();
            }

            IUnitOfWork unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();

            if (opts.ListVisits)
            {
                // no need to log
                ListVisits(unitOfWork);
                return;
            }

            IFileSystemIOWrapper fileSystemIOWrapper = serviceProvider.GetRequiredService<IFileSystemIOWrapper>();
            IAppSettingsManager appSettingsManager = serviceProvider.GetRequiredService<IAppSettingsManager>();

            // easier to see the beginning of each app run in the log files
            Log.Information("----------------------------------");
            Log.Information("------------ FilmCRUD ------------");
            Log.Information("----------------------------------");

            if (opts.ListContents)
            {
                var visitCrudManager = new VisitCRUDManager(unitOfWork, fileSystemIOWrapper, appSettingsManager);

                Log.Information(
                    "Will access the following storage directory, press \"y\" to confirm, other key to deny: {DirPath}",
                    visitCrudManager.MovieWarehouseDirectory);
                bool toContinue = Console.ReadLine().Trim().ToLower() == "y";
                if (!toContinue)
                {
                    Log.Information("Quitting...");
                    return;
                }
                visitCrudManager.WriteMovieWarehouseContentsToTextFile();
            }
            else if (!string.IsNullOrEmpty(opts.PersistContents))
            {
                string visitDateString = opts.PersistContents;
                ILogger parsingErrorsLogger = GetLoggerForParsingErrors(visitDateString);

                Log.Information("Persisting warehouse contents for date {VisitDateString}", visitDateString);
                var visitCrudManager = new VisitCRUDManager(unitOfWork, fileSystemIOWrapper, appSettingsManager, parsingErrorsLogger);
                visitCrudManager.ReadWarehouseContentsAndRegisterVisit(visitDateString);
            }
            else if (!string.IsNullOrEmpty(opts.ProcessManual))
            {
                string visitDateString = opts.ProcessManual;
                ILogger parsingErrorsLogger = GetLoggerForParsingErrors(visitDateString);

                Log.Information("Processing manually configured movie files for visit date {VisitDateString}", visitDateString);
                var visitCrudManager = new VisitCRUDManager(unitOfWork, fileSystemIOWrapper, appSettingsManager, parsingErrorsLogger);
                visitCrudManager.ProcessManuallyProvidedMovieRipsForExistingVisit(visitDateString);
            }
            else
            {
                Log.Information("No action requested...");
            }
        }

        // not a persistent method and so does not log
        private static void HandleScanRipsOptions(ScanRipsOptions opts, ServiceProvider serviceProvider)
        {
            IUnitOfWork unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();

            Console.WriteLine("------------");
            if (opts.ListVisits)
            {
                ListVisits(unitOfWork);
                return;
            }

            var scanRipsManager = new ScanRipsManager(unitOfWork);

            // defaults to the latest visit if opts.Visit == null
            MovieWarehouseVisit visit = GetClosestMovieWarehouseVisit(scanRipsManager, opts.Visit);

            string printDateFormat = "MMMM dd yyyy";
            if (opts.CountByReleaseDate)
            {
                Console.WriteLine($"Visit: {visit.VisitDateTime.ToString(printDateFormat)}");
                Console.WriteLine("ScanRips: count by ReleaseDate\n");
                Dictionary<string, int> countByRelaseDate = scanRipsManager.GetRipCountByReleaseDate(visit);
                foreach (var kv in countByRelaseDate.OrderBy(kv => kv.Key))
                {
                    Console.WriteLine($"{kv.Key}: {kv.Value}");
                }
            }
            else if (opts.WithDates.Any())
            {
                Console.WriteLine($"Visit: {visit.VisitDateTime.ToString(printDateFormat)}");
                string releaseDates = string.Join(" or ", opts.WithDates);
                Console.WriteLine($"ScanRips: rips with ReleaseDate {releaseDates}\n");
                List<string> ripFileNames = scanRipsManager
                    .GetAllRipsWithReleaseDate(visit, opts.WithDates.ToArray())
                    .ToList();

                Console.WriteLine($"Total count: {ripFileNames.Count()}\n");

                foreach (var fileName in ripFileNames.OrderBy(r => r))
                {
                    Console.WriteLine(fileName);
                }
            }
            else if (opts.CountByVisit)
            {
                Console.WriteLine("ScanRips: count by visit \n");
                Dictionary<DateTime, int> countByVisit = scanRipsManager.GetRipCountByVisit();
                foreach (var item in countByVisit.OrderBy(kvp => kvp.Key))
                {
                    string visitStr = item.Key.ToString("MMMM dd yyyy");
                    Console.WriteLine($"{visitStr} : {item.Value}");
                }
            }
            else if (opts.VisitDiff.Any())
            {
                GetVisitDiffAndPrint(
                    scanRipsManager,
                    opts.VisitDiff,
                    visitDiffStrategy: scanRipsManager.GetVisitDiff,
                    printDateFormat: printDateFormat);
            }
            else if (opts.LastVisitDiff)
            {
                Console.WriteLine("ScanRips: last visit difference \n");
                PrintVisitDiff(scanRipsManager.GetLastVisitDiff());
            }
            else
            {
                Console.WriteLine("No action requested...");
            }
            Console.WriteLine("------------");
        }

        private static void HandleScanMoviesOptions(ScanMoviesOptions opts, ServiceProvider serviceProvider)
        {
            IUnitOfWork unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            var scanMoviesManager = new ScanMoviesManager(unitOfWork);

            Console.WriteLine("-------------");
            if (opts.ListVisits)
            {
                ListVisits(unitOfWork);
                return;
            }

            // defaults to the latest visit if opts.Visit == null
            MovieWarehouseVisit visit = GetClosestMovieWarehouseVisit(scanMoviesManager, opts.Visit);

            string printDateFormat = "MMMM dd yyyy";
            Console.WriteLine($"Visit: {visit.VisitDateTime.ToString(printDateFormat)}");
            if (opts.WithGenres.Any())
            {
                // finds the Genre entities for each string in opts.WithGenres, then flattens
                IEnumerable<Genre> genres = opts.WithGenres
                    .Select(name => scanMoviesManager.GenresFromName(name))
                    .SelectMany(g => g);
                IEnumerable<Movie> moviesWithGenres = scanMoviesManager.GetMoviesWithGenres(visit, genres.ToArray());
                string genreNames = string.Join(" | ", genres.Select(g => g.Name));
                Console.WriteLine($"Movies with genres: {genreNames} \n");
                moviesWithGenres.ToList().ForEach(m => Console.WriteLine("-------------" + '\n' + m.PrettyFormat()));
            }
            else if (opts.WithActors.Any())
            {
                // finds the Actor entities for each string in opts.WithActors, then flattens
                IEnumerable<Actor> actors = opts.WithActors
                    .Select(name => scanMoviesManager.GetActorsFromName(name))
                    .SelectMany(a => a);
                IEnumerable<Movie> moviesWithActors = scanMoviesManager.GetMoviesWithActors(visit, actors.ToArray());
                string actorNames = string.Join(" | ", actors.Select(a => a.Name));
                Console.WriteLine($"Movies with actors: {actorNames} \n");
                moviesWithActors.ToList().ForEach(m => Console.WriteLine("-------------" + '\n' + m.PrettyFormat()));
            }
            else if (opts.WithDirectors.Any())
            {
                // finds the Director entities for each string in opts.WithDirectors, then flattens
                IEnumerable<Director> directors = opts.WithDirectors
                    .Select(name => scanMoviesManager.GetDirectorsFromName(name))
                    .SelectMany(a => a);
                IEnumerable<Movie> moviesWithDirectors = scanMoviesManager.GetMoviesWithDirectors(visit, directors.ToArray());
                string directorNames = string.Join(" | ", directors.Select(d => d.Name));
                Console.WriteLine($"Movies with directors: {directorNames} \n");
                moviesWithDirectors.ToList().ForEach(m => Console.WriteLine("-------------" + '\n' + m.PrettyFormat()));
            }
            else if (opts.WithDates.Any())
            {
                IEnumerable<Movie> moviesWithDates = scanMoviesManager.GetMoviesWithReleaseDates(visit, opts.WithDates.ToArray());
                string releaseDates = string.Join(" or ", opts.WithDates);
                Console.WriteLine($"Movies with release date {releaseDates}: \n");
                moviesWithDates.ToList().ForEach(m => Console.WriteLine("-------------" + '\n' + m.PrettyFormat()));
            }
            else if (opts.ByGenre)
            {
                Console.WriteLine("Count by genre:\n");
                IEnumerable<KeyValuePair<Genre, int>> genreCount = scanMoviesManager.GetCountByGenre(visit);
                int toTake = opts.Top ?? genreCount.Count();
                genreCount.OrderByDescending(kvp => kvp.Value).ThenBy(kvp => kvp.Key.Name)
                    .Take(toTake)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key.Name}: {kvp.Value}"));
            }
            else if (opts.ByActor)
            {
                Console.WriteLine("Count by actor:\n");
                IEnumerable<KeyValuePair<Actor, int>> actorCount = scanMoviesManager.GetCountByActor(visit);
                int toTake = opts.Top ?? actorCount.Count();
                actorCount.OrderByDescending(kvp => kvp.Value).ThenBy(kvp => kvp.Key.Name)
                    .Take(toTake)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key.Name}: {kvp.Value}"));
            }
            else if (opts.ByDirector)
            {
                Console.WriteLine("Count by director:\n");
                IEnumerable<KeyValuePair<Director, int>> directorCount = scanMoviesManager.GetCountByDirector(visit);
                int toTake = opts.Top ?? directorCount.Count();
                directorCount.OrderByDescending(kvp => kvp.Value).ThenBy(kvp => kvp.Key.Name)
                    .Take(toTake)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key.Name}: {kvp.Value}"));
            }
            else if (opts.Search != null)
            {
                string toSearch = opts.Search;
                Console.WriteLine($"Search by title: \"{toSearch}\" \n");
                IEnumerable<Movie> searchResult = scanMoviesManager.SearchMovieEntitiesByTitle(visit, toSearch);
                if (!searchResult.Any())
                {
                    Console.WriteLine("No matches...");
                }
                else
                {
                    searchResult.ToList().ForEach(m => Console.WriteLine("-------------" + '\n' + m.PrettyFormat()));
                }
            }
            else if (opts.LastVisitDiff)
            {
                PrintVisitDiff(scanMoviesManager.GetLastVisitDiff());
            }
            else if (opts.VisitDiff.Any())
            {
                GetVisitDiffAndPrint(
                    scanMoviesManager,
                    opts.VisitDiff,
                    visitDiffStrategy: scanMoviesManager.GetVisitDiff,
                    printDateFormat: printDateFormat);
            }
            else
            {
                Console.WriteLine("No action requested...");
            }
            Console.WriteLine();
        }

        private static async Task HandleLinkOptions(LinkOptions opts, ServiceProvider serviceProvider)
        {
            // default MinimumLevel is Information
            ILogger linkingErrorsLogger = new LoggerConfiguration()
                    .WriteTo.File(
                        "logs/linking_errors_.txt",
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: _logOutputTemplate)
                    .CreateLogger();

            var ripToMovieLinker = new RipToMovieLinker(
                serviceProvider.GetRequiredService<IUnitOfWork>(),
                serviceProvider.GetRequiredService<IAppSettingsManager>(),
                serviceProvider.GetRequiredService<IMovieAPIClient>(),
                linkingErrorsLogger);

            // easier to see the beginning of each app run in the log files
            Log.Information("----------------------------------");
            Log.Information("------------ FilmCRUD ------------");
            Log.Information("----------------------------------");

            if (opts.Search)
            {
                Log.Information("Linking movie rips to movies - searching...");
                await ripToMovieLinker.SearchAndLinkAsync(opts.MaxCalls ?? -1);
            }
            else if (opts.FromManualExtIds)
            {
                Log.Information($"Linking movie rips to movies - from manually configured external ids...");
                await ripToMovieLinker.LinkFromManualExternalIdsAsync(opts.MaxCalls ?? -1);
            }
            else if (opts.GetUnlinkedRips)
            {
                IEnumerable<string> unlinked = ripToMovieLinker.GetAllUnlinkedMovieRips();
                Console.WriteLine($"Unlinked MovieRips:");
                Console.WriteLine();
                unlinked.ToList().ForEach(s => Console.WriteLine(s));
            }
            else if (opts.ValidateManualExtIds)
            {
                Log.Information($"Validating manually configured external ids...");
                await ripToMovieLinker.ValidateManualExternalIdsAsync(opts.MaxCalls ?? -1);
            }
            else
            {
                Log.Information("No action requested...");
            }
        }

        public static async Task HandleFetchOptions(FetchOptions opts, ServiceProvider serviceProvider)
        {
            // local func to create the logger for each fetcher class;
            // made static since it does not need local variables or instance state
            static ILogger GetLoggerForFetchingErrors(string filePath)
            {
                // default MinimumLevel is Information
                return new LoggerConfiguration()
                    .WriteTo.File(
                        filePath,
                        rollingInterval: RollingInterval.Day,
                        outputTemplate: _logOutputTemplate)
                    .CreateLogger();
            }

            IUnitOfWork unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            IAppSettingsManager appSettingsManager = serviceProvider.GetRequiredService<IAppSettingsManager>();
            IMovieAPIClient movieAPIClient = serviceProvider.GetRequiredService<IMovieAPIClient>();

            // easier to see the beginning of each app run in the log files
            Log.Information("----------------------------------");
            Log.Information("------------ FilmCRUD ------------");
            Log.Information("----------------------------------");

            if (opts.Genres)
            {
                ILogger fetchingErrorsLogger = GetLoggerForFetchingErrors("logs/fetching_errors_genres_.txt");
                var genresFetcher = new MovieDetailsFetcherGenres(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger);
                Log.Information("Fetching genres for movies...");
                await genresFetcher.PopulateDetails(opts.MaxCalls ?? -1);
            }
            else if (opts.Actors)
            {
                ILogger fetchingErrorsLogger = GetLoggerForFetchingErrors("logs/fetching_errors_actors_.txt");
                var actorsFetcher = new MovieDetailsFetcherActors(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger);
                Log.Information("Fetching actors for movies...");
                await actorsFetcher.PopulateDetails(opts.MaxCalls ?? -1);
            }
            else if (opts.Directors)
            {
                ILogger fetchingErrorsLogger = GetLoggerForFetchingErrors("logs/fetching_errors_directors_.txt");
                var directorsFetcher = new MovieDetailsFetcherDirectors(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger);
                Log.Information("Fetching directors for movies...");
                await directorsFetcher.PopulateDetails(opts.MaxCalls ?? -1);
            }
            else if (opts.Keywords)
            {
                ILogger fetchingErrorsLogger = GetLoggerForFetchingErrors("logs/fetching_errors_keywords_.txt");
                var keywordsFetcher = new MovieDetailsFetcherSimple(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger);
                Log.Information("Fetching keywords for movies...");
                await keywordsFetcher.PopulateMovieKeywords();
            }
            else if (opts.IMDBIds)
            {
                ILogger fetchingErrorsLogger = GetLoggerForFetchingErrors("logs/fetching_errors_imdbids_.txt");
                var IMDBIdFetcher = new MovieDetailsFetcherSimple(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger);
                Log.Information("Fetching IMDB ids for movies...");
                await IMDBIdFetcher.PopulateMovieIMDBIdsAsync();
            }
            else
            {
                Log.Information("No fetch request...");
            }
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var errorObj in errors)
            {
                Console.WriteLine(errorObj.Tag);
            }
        }

        private static MovieWarehouseVisit GetClosestMovieWarehouseVisit(GeneralScanManager scanManager, string dateString)
        {
            MovieWarehouseVisit visit;
            if (dateString == null)
            {
                visit = scanManager.GetClosestVisit();
            }
            else
            {
                DateTime visitDate = DateTime.ParseExact(dateString, "yyyyMMdd", null);
                visit = scanManager.GetClosestVisit(visitDate);
            }
            return visit;
        }

        /// <summary>
        /// Works for both ScanMoviesManager.GetVisitDiff and ScanRipsManager.GetVisitDiff.
        /// Parameter <paramref name="diffDates"/> should be as is provided in ScanMoviesOptions.VisitDiff
        /// and ScanRipsOptions.VisitDiff respectively.
        /// </summary>
        private static void GetVisitDiffAndPrint(
            GeneralScanManager scanManager,
            IEnumerable<string> diffDates,
            Func<MovieWarehouseVisit, MovieWarehouseVisit, Dictionary<string, IEnumerable<string>>> visitDiffStrategy,
            string printDateFormat)
        {
            IEnumerable<int> dateInts = diffDates.Select(i => int.Parse(i)).OrderByDescending(i => i);

            int dateIntRight = dateInts.First();
            MovieWarehouseVisit visitRight = GetClosestMovieWarehouseVisit(scanManager, dateIntRight.ToString());

            int dateIntLeft = dateInts.Skip(1).FirstOrDefault();
            MovieWarehouseVisit visitLeft;
            if (dateIntLeft > 0)
            {
                visitLeft = GetClosestMovieWarehouseVisit(scanManager, dateIntLeft.ToString());
            }
            else
            {
                visitLeft = scanManager.GetPreviousVisit(visitRight);
            }

            string _left = visitLeft.VisitDateTime.ToString(printDateFormat);
            string _right = visitRight.VisitDateTime.ToString(printDateFormat);
            Console.WriteLine($"Visit Difference: {_left} -> {_right}");
            PrintVisitDiff(visitDiffStrategy(visitLeft, visitRight));
        }

        private static void ListVisits(IUnitOfWork unitOfWork)
        {
            Console.WriteLine("Dates for all warehouse visits:");
            unitOfWork.MovieWarehouseVisits.GetVisitDates()
                .OrderByDescending(dt => dt)
                .ToList()
                .ForEach(dt => Console.WriteLine($"{dt.ToString("MMMM dd yyyy")} - {dt:yyyyMMdd}"));
        }

        private static void PrintVisitDiff(Dictionary<string, IEnumerable<string>> visitDiff)
        {
            foreach (var item in visitDiff.OrderBy(kvp => kvp.Key))
            {
                Console.WriteLine("\n----------");
                Console.WriteLine($"{item.Key} | Count: {item.Value.Count()}");
                Console.WriteLine(String.Join('\n', item.Value.OrderBy(s => s)));
            }
        }

    }

}

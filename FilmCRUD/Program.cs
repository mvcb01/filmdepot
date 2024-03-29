﻿using System.Linq;
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
        /// <summary>
        /// Default serilog template minus the timezone.
        /// </summary>
        private const string _logOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Handles the options defined in verbs <see cref="VisitOptions"/>, <see cref="ScanRipsOptions"/>, <see cref="ScanMoviesOptions"/>
        /// <see cref="LinkOptions"/> and <see cref="FetchOptions"/>.
        /// </summary>
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

        /// <summary>
        /// To handle options defined in verb <see cref="VisitOptions"/>.
        /// </summary>
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

        /// <summary>
        /// To handle options defined in verb <see cref="ScanRipsOptions"/>. Not a peristent method so there's no logging.
        /// </summary>
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

            // defaults to the latest visit if opts.Visit is null
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
                Console.WriteLine($"ScanRips: rips with ParsedReleaseDate {releaseDates}\n");
                List<string> ripFileNames = scanRipsManager
                    .GetAllRipsWithReleaseDate(visit, opts.WithDates.ToArray())
                    .ToList();

                Console.WriteLine($"Total count: {ripFileNames.Count()}\n");

                foreach (var fileName in ripFileNames.OrderBy(r => r))
                {
                    Console.WriteLine(fileName);
                }
            }
            else if (opts.WithGroup is not null)
            {
                Console.WriteLine($"Visit: {visit.VisitDateTime.ToString(printDateFormat)}");

                IEnumerable<MovieRip> ripsWithGroup = scanRipsManager.GetRipsWithRipGroup(visit, opts.WithGroup);

                Console.WriteLine($"ScanRips: rips with release group {opts.WithGroup}\n");
                ripsWithGroup.OrderBy(mr => mr.FileName).ToList().ForEach(mr => Console.WriteLine(mr.FileName));
            }
            else if (opts.ByVisit)
            {
                Console.WriteLine("ScanRips: count by visit \n");
                Dictionary<DateTime, int> countByVisit = scanRipsManager.GetRipCountByVisit();
                foreach (var item in countByVisit.OrderBy(kvp => kvp.Key))
                {
                    string visitStr = item.Key.ToString("MMMM dd yyyy");
                    Console.WriteLine($"{visitStr} : {item.Value}");
                }
            }
            else if (opts.ByGroup)
            {
                Console.WriteLine($"Visit: {visit.VisitDateTime.ToString(printDateFormat)}");

                IEnumerable<KeyValuePair<string, int>> countByGroup = scanRipsManager.GetRipCountByRipGroup(visit);
                int topN = opts.Top ?? countByGroup.Count();
                Console.WriteLine("ScanRips: count by release group \n");
                countByGroup
                    .OrderByDescending(kvp => kvp.Value)
                    .Take(topN)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key}: {kvp.Value}"));
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
            else if (opts.Search is not null)
            {
                string toSearch = opts.Search;
                Console.WriteLine($"Visit: {visit.VisitDateTime.ToString(printDateFormat)}");
                Console.WriteLine($"Search by filename tokens: \"{toSearch}\" \n");
                IEnumerable<MovieRip> searchResult = scanRipsManager.SearchFromFileNameTokens(visit, toSearch);
                if (!searchResult.Any())
                {
                    Console.WriteLine("No matches...");
                }
                else
                {
                    searchResult.ToList().ForEach(mr => Console.WriteLine("-------------" + '\n' + mr.PrettyFormat()));
                }
            }
            else
            {
                Console.WriteLine("No action requested...");
            }
            Console.WriteLine("------------");
        }

        /// <summary>
        /// To handle options defined in verb <see cref="ScanMoviesOptions "/>. Not a peristent method so there's no logging.
        /// </summary>
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

            // defaults to the latest visit if opts.Visit is null
            MovieWarehouseVisit visit = GetClosestMovieWarehouseVisit(scanMoviesManager, opts.Visit);

            string printDateFormat = "MMMM dd yyyy";
            Console.WriteLine($"Target visit: {visit.VisitDateTime.ToString(printDateFormat)}");
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
            else if (opts.WithCast.Any())
            {
                // finds the CastMember entities for each string in opts.CastMembers, then flattens
                IEnumerable<CastMember> castMembers = opts.WithCast
                    .Select(name => scanMoviesManager.GetCastMembersFromName(name))
                    .SelectMany(a => a);
                IEnumerable<Movie> moviesWithCastMembers = scanMoviesManager.GetMoviesWithCastMembers(visit, castMembers.ToArray());
                string castMemberNames = string.Join(" | ", castMembers.Select(a => a.Name));
                Console.WriteLine($"Movies with cast: {castMemberNames} \n");
                moviesWithCastMembers.ToList().ForEach(m => Console.WriteLine("-------------" + '\n' + m.PrettyFormat()));
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
            else if (opts.WithKeywords.Any())
            {
                IEnumerable<Movie> moviesWithKeywords = scanMoviesManager.GetMoviesWithKeywords(visit, opts.WithKeywords.ToArray());
                string kwds = string.Join(" or ", opts.WithKeywords.Select(k => "\"" + k + "\""));
                Console.WriteLine($"Movies with keywords {kwds}: \n");
                moviesWithKeywords.ToList().ForEach(m => Console.WriteLine("-------------" + '\n' + m.PrettyFormat()));
            }
            else if (opts.ByGenre)
            {
                Console.WriteLine("Count by genre:\n");
                IEnumerable<KeyValuePair<Genre, int>> genreCount = scanMoviesManager.GetCountByGenre(visit, out int withoutGenres);
                int toTake = opts.Top ?? genreCount.Count();
                genreCount
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenBy(kvp => kvp.Key.Name)
                    .Take(toTake)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key.Name}: {kvp.Value}"));
                Console.WriteLine($"<empty>: {withoutGenres}");
            }
            else if (opts.ByCastMember)
            {
                Console.WriteLine("Count by cast member:\n");
                IEnumerable<KeyValuePair<CastMember, int>> castMemberCount = scanMoviesManager.GetCountByCastMember(visit, out int withoutCastMembers);
                int toTake = opts.Top ?? castMemberCount.Count();
                castMemberCount
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenBy(kvp => kvp.Key.Name)
                    .Take(toTake)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key.Name}: {kvp.Value}"));
                Console.WriteLine($"<empty>: {withoutCastMembers}");
            }
            else if (opts.ByDirector)
            {
                Console.WriteLine("Count by director:\n");
                IEnumerable<KeyValuePair<Director, int>> directorCount = scanMoviesManager.GetCountByDirector(visit, out int withoutDirectors);
                int toTake = opts.Top ?? directorCount.Count();
                directorCount
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenBy(kvp => kvp.Key.Name)
                    .Take(toTake)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key.Name}: {kvp.Value}"));
                Console.WriteLine($"<empty>: {withoutDirectors}");
            }
            else if (opts.ByReleaseDate)
            {
                Console.WriteLine("Count by release date:\n");
                IEnumerable<KeyValuePair<int, int>> releaseDateCount = scanMoviesManager.GetCountbyReleaseDate(visit, out int withoutReleaseDateCount);
                int toTake = opts.Top ?? releaseDateCount.Count();
                releaseDateCount
                    .OrderBy(kvp => kvp.Key) // console prints are ordered by release date ascending
                    .Take(toTake)
                    .ToList()
                    .ForEach(kvp => Console.WriteLine($"{kvp.Key}: {kvp.Value}"));
                Console.WriteLine($"<empty>: {withoutReleaseDateCount}");
            }
            else if (opts.Search is not null)
            {
                string toSearch = opts.Search;
                Console.WriteLine($"Search by title: \"{toSearch}\" \n");
                IEnumerable<Movie> searchResult = scanMoviesManager.SearchMovieEntities(visit, toSearch);
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

        /// <summary>
        /// To handle options defined in verb <see cref="LinkOptions"/>. This is an asynchronous method since, depending
        /// on parameter <paramref name="opts"/>, <see cref="IMovieAPIClient"/> may be used.
        /// </summary>
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

        /// <summary>
        /// To handle options defined in verb <see cref="FetchOptions"/>. This is an asynchronous method
        /// since <see cref="IMovieAPIClient"/> will be used.
        /// </summary>
        private static async Task HandleFetchOptions(FetchOptions opts, ServiceProvider serviceProvider)
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
            else if (opts.CastMembers)
            {
                ILogger fetchingErrorsLogger = GetLoggerForFetchingErrors("logs/fetching_errors_castmembers_.txt");
                var castMembersFetcher = new MovieDetailsFetcherCastMembers(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger);
                Log.Information("Fetching cast members for movies...");
                await castMembersFetcher.PopulateDetails(opts.MaxCalls ?? -1);
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
                await keywordsFetcher.PopulateMovieKeywordsAsync(opts.MaxCalls ?? -1);
            }
            else if (opts.IMDBIds)
            {
                ILogger fetchingErrorsLogger = GetLoggerForFetchingErrors("logs/fetching_errors_imdbids_.txt");
                var IMDBIdFetcher = new MovieDetailsFetcherSimple(unitOfWork, appSettingsManager, movieAPIClient, fetchingErrorsLogger);
                Log.Information("Fetching IMDB ids for movies...");
                await IMDBIdFetcher.PopulateMovieIMDBIdsAsync(opts.MaxCalls ?? -1);
            }
            else
            {
                Log.Information("No fetch request...");
            }
        }

        /// <summary>
        /// To handle errors on parsing the commands line args from <see cref="Main"/>.
        /// </summary>
        /// <param name="errors"></param>
        private static void HandleParseError(IEnumerable<Error> errors)
        {
            string msg;
            foreach (var errorObj in errors)
            {
                msg = errorObj is TokenError tokenError ? $"{tokenError.Tag}: {tokenError.Token}" : $"{errorObj.Tag}";
                Console.WriteLine(msg);
            }
        }

        private static MovieWarehouseVisit GetClosestMovieWarehouseVisit(GeneralScanManager scanManager, string dateString)
        {
            MovieWarehouseVisit visit;
            if (dateString is null)
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
        /// Works for both methods <see cref="ScanMoviesManager.GetVisitDiff"/> and <see cref="ScanRipsManager.GetVisitDiff"/>.
        /// Parameter <paramref name="diffDates"/> should be as is provided in <see cref="ScanMoviesOptions.VisitDiff"/>
        /// and <see cref="ScanRipsOptions.VisitDiff"/> respectively.
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

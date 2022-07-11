using CommandLine;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

using FilmDataAccess.EFCore.UnitOfWork;
using FilmDomain.Interfaces;
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
        static async Task Main(string[] args)
        {
            ServiceCollection services = new();
            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IUnitOfWork unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            IFileSystemIOWrapper fileSystemIOWrapper = serviceProvider.GetRequiredService<IFileSystemIOWrapper>();
            IAppSettingsManager appSettingsManager = serviceProvider.GetRequiredService<IAppSettingsManager>();
            IMovieAPIClient movieAPIClient = serviceProvider.GetRequiredService<IMovieAPIClient>();

            VisitCRUDManager visitCrudManager = new(unitOfWork, fileSystemIOWrapper, appSettingsManager);
            ScanRipsManager scanRipsManager = new(unitOfWork);
            RipToMovieLinker ripToMovieLinker = new(unitOfWork, fileSystemIOWrapper, appSettingsManager, movieAPIClient);

            ParserResult<object> parsed = Parser
                .Default
                .ParseArguments<VisitOptions, ScanRipsOptions, LinkOptions, FetchOptions>(args);
            parsed.WithParsed<VisitOptions>(opts => HandleVisitOptions(opts, visitCrudManager));
            parsed.WithParsed<ScanRipsOptions>(opts => HandleScanRipsOptions(opts, scanRipsManager));
            await parsed.WithParsedAsync<LinkOptions>(async opts => await HandleLinkOptions(opts, ripToMovieLinker));
            await parsed.WithParsedAsync<FetchOptions>(async opts => await HandleFetchOptions(opts, unitOfWork, movieAPIClient));
            parsed.WithNotParsed(HandleParseError);
            {}
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IUnitOfWork, SQLiteUnitOfWork>(_ => new SQLiteUnitOfWork(new SQLiteAppContext()));

            services.AddSingleton<IFileSystemIOWrapper, FileSystemIOWrapper>();

            services.AddSingleton<IAppSettingsManager, AppSettingsManager>();

            AppSettingsManager _appSettingsManager = new();
            string apiKey = _appSettingsManager.GetApiKey("TheMovieDb");
            services.AddSingleton<IMovieAPIClient, TheMovieDbAPIClient>(_ => new TheMovieDbAPIClient(apiKey));
        }

        private static void HandleVisitOptions(VisitOptions visitOpts, VisitCRUDManager visitCrudManager)
        {
            if (visitOpts.ListContents)
            {
                System.Console.WriteLine("-------------");
                System.Console.WriteLine($"Movie warehouse: {visitCrudManager.MovieWarehouseDirectory}");
                System.Console.WriteLine("Garantir que o disco está ligado... Y para sim, outra para não.");
                bool toContinue = Console.ReadLine().Trim().ToLower() == "y";
                if (!toContinue)
                {
                    System.Console.WriteLine("A sair...");
                    return;
                }
                visitCrudManager.WriteMovieWarehouseContentsToTextFile();
            }
            else if (!string.IsNullOrEmpty(visitOpts.PersistContents))
            {
                visitCrudManager.ReadWarehouseContentsAndRegisterVisit(visitOpts.PersistContents, failOnParsingErrors: false);
            }
            else
            {
                System.Console.WriteLine("No action requested...");
            }
        }

        private static void HandleScanRipsOptions(ScanRipsOptions scanRipsOpts, ScanRipsManager scanRipsManager)
        {
            System.Console.WriteLine("----------");
            if (scanRipsOpts.CountByReleaseDate)
            {
                System.Console.WriteLine("Scan: contagem por ReleaseDate\n");
                Dictionary<string, int> countByRelaseDate = scanRipsManager.GetRipCountByReleaseDate();
                foreach (var kv in countByRelaseDate.OrderBy(kv => kv.Key))
                {
                    System.Console.WriteLine($"{kv.Key}: {kv.Value}");
                }
            }
            else if (scanRipsOpts.WithReleaseDate != null)
            {
                System.Console.WriteLine($"Scan: rips com ReleaseDate {scanRipsOpts.WithReleaseDate}\n");
                List<string> ripFileNames = scanRipsManager.GetAllRipsWithReleaseDate(scanRipsOpts.WithReleaseDate).ToList();

                System.Console.WriteLine($"Contagem: {ripFileNames.Count()}\n");

                foreach (var fileName in ripFileNames.OrderBy(r => r))
                {
                    System.Console.WriteLine(fileName);
                }
            }
            else if (scanRipsOpts.CountByVisit)
            {
                System.Console.WriteLine("Scan: contagem por visita\n");
                Dictionary<DateTime, int> countByVisit = scanRipsManager.GetRipCountByVisit();
                foreach (var item in countByVisit.OrderBy(kvp => kvp.Key))
                {
                    string visitStr = item.Key.ToString("yyyyMMdd");
                    System.Console.WriteLine($"{visitStr} : {item.Value}");
                }
            }
            else if (scanRipsOpts.LastVisitDiff)
            {
                System.Console.WriteLine("Scan: diff da última visita\n");
                Dictionary<string, IEnumerable<string>> lastVisitDiff = scanRipsManager.GetLastVisitDiff();
                foreach (var item in lastVisitDiff.OrderBy(kvp => kvp.Key))
                {
                    System.Console.WriteLine("\n----------");
                    System.Console.WriteLine(item.Key + "\n");
                    System.Console.WriteLine(String.Join('\n', item.Value.OrderBy(s => s)));
                }
            }
            else
            {
                System.Console.WriteLine("No action requested...");
            }
            System.Console.WriteLine();
        }

        private static async Task HandleLinkOptions(LinkOptions opts, RipToMovieLinker ripToMovieLinker)
        {
            System.Console.WriteLine("-------------");
            if (opts.Search)
            {
                System.Console.WriteLine($"A linkar...");
                await ripToMovieLinker.SearchAndLinkAsync();
            }
            else if (opts.FromManualExtIds)
            {
                System.Console.WriteLine($"A linkar a partir de external ids manuais...");
                await ripToMovieLinker.LinkFromManualExternalIdsAsync();
            }
            else if (opts.GetUnlinkedRips)
            {
                IEnumerable<string> unlinked = ripToMovieLinker.GetAllUnlinkedMovieRips();
                System.Console.WriteLine($"MovieRips não linkados:");
                System.Console.WriteLine();
                unlinked.ToList().ForEach(s => System.Console.WriteLine(s));

            }
            else if (opts.ValidateManualExtIds)
            {
                System.Console.WriteLine($"A validar external ids manuais:");
                System.Console.WriteLine();
                Dictionary<string, Dictionary<string, int>> validStatus = await ripToMovieLinker.ValidateManualExternalIdsAsync();
                foreach (var item in validStatus)
                {
                    Dictionary<string, int> innerDict = item.Value;

                    System.Console.WriteLine(item.Key);
                    IEnumerable<string> linesToPrint = innerDict.Select(kvp => $"{kvp.Key} : {kvp.Value}");
                    System.Console.WriteLine(string.Join('\n', linesToPrint));
                    System.Console.WriteLine();
                }
            }
            else
            {
                System.Console.WriteLine("No action requested...");
            }
            System.Console.WriteLine();

        }

        public static async Task HandleFetchOptions(FetchOptions opts, IUnitOfWork unitOfWork, IMovieAPIClient movieAPIClient)
        {
            if (opts.Genres)
            {
                var genresFetcher = new MovieDetailsFetcherGenres(unitOfWork, movieAPIClient);
                System.Console.WriteLine("fetching genres for movies...");
                await genresFetcher.PopulateDetails();
            }
            else if (opts.Actors)
            {
                var actorsFetcher = new MovieDetailsFetcherActors(unitOfWork, movieAPIClient);
                System.Console.WriteLine("fetching actors for movies...");
                await actorsFetcher.PopulateDetails();
            }
            else if (opts.Directors)
            {
                var directorsFetcher = new MovieDetailsFetcherDirectors(unitOfWork, movieAPIClient);
                System.Console.WriteLine("fetching directors for movies...");
                await directorsFetcher.PopulateDetails();
            }
            else if (opts.Keywords)
            {
                var keywordsFetcher = new MovieDetailsFetcherSimple(unitOfWork, movieAPIClient);
                System.Console.WriteLine("fetching keywords for movies...");
                await keywordsFetcher.PopulateMovieKeywords();
            }
            else if (opts.IMDBIds)
            {
                var IMDBIdFetcher = new MovieDetailsFetcherSimple(unitOfWork, movieAPIClient);
                System.Console.WriteLine("fetching imdb ids for movies...");
                await IMDBIdFetcher.PopulateMovieIMDBIds();
            }
            else
            {
                System.Console.WriteLine("No fetch request...");
            }
            System.Console.WriteLine();
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var errorObj in errors)
            {
                System.Console.WriteLine(errorObj.Tag);
            }
        }

    }
}

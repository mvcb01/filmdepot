using Microsoft.Extensions.DependencyInjection;
using FilmDataAccess.EFCore.UnitOfWork;
using FilmDomain.Interfaces;
using FilmDataAccess.EFCore;

using CommandLine;
using System.Linq;
using System.Collections.Generic;
using System;

using FilmCRUD.Verbs;
using FilmCRUD.Interfaces;
using ConfigUtils;
using ConfigUtils.Interfaces;

namespace FilmCRUD
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceCollection services = new();
            ConfigureServices(services);
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IUnitOfWork unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
            IFileSystemIOWrapper fileSystemIOWrapper = serviceProvider.GetRequiredService<IFileSystemIOWrapper>();
            IAppSettingsManager appSettingsManager = serviceProvider.GetRequiredService<IAppSettingsManager>();

            VisitCRUDManager visitCrudManager = new(unitOfWork, fileSystemIOWrapper, appSettingsManager);
            ScanManager scanManager = new(unitOfWork);

            Parser.Default.ParseArguments<VisitOptions, ScanRipsOptions>(args)
                .WithParsed<VisitOptions>(opts => HandleVisitOptions(opts, visitCrudManager))
                .WithParsed<ScanRipsOptions>(opts => HandleScanRipsOptions(opts, scanManager))
                .WithNotParsed(HandleParseError);
            {}
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IUnitOfWork, SQLiteUnitOfWork>(
                _ => new SQLiteUnitOfWork(new SQLiteAppContext())
                );

            services.AddSingleton<IFileSystemIOWrapper, FileSystemIOWrapper>();

            services.AddSingleton<IAppSettingsManager, AppSettingsManager>(
                _ => new AppSettingsManager("appsettings.json")
                );
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
            else if (visitOpts.PersistContents)
            {
                if (visitOpts.ContentsDate == null)
                {
                    throw new ArgumentNullException("Deve ser dado o argumento contentsdate");
                }
                visitCrudManager.ReadWarehouseContentsAndRegisterVisit(visitOpts.ContentsDate, failOnParsingErrors: false);
            }
            else
            {
                System.Console.WriteLine("Nada a fazer...");
            }
        }

        public static void HandleScanRipsOptions(ScanRipsOptions scanRipsOpts, ScanManager scanManager)
        {
            System.Console.WriteLine("----------");
            if (scanRipsOpts.CountByReleaseDate)
            {
                System.Console.WriteLine("Scan: contagem por ReleaseDate\n");
                Dictionary<string, int> countByRelaseDate = scanManager.GetRipCountByReleaseDate();
                foreach (var kv in countByRelaseDate.OrderBy(kv => kv.Key))
                {
                    System.Console.WriteLine($"{kv.Key}: {kv.Value}");
                }
            }
            else if (scanRipsOpts.WithReleaseDate != null)
            {
                System.Console.WriteLine($"Scan: rips com ReleaseDate {scanRipsOpts.WithReleaseDate}\n");
                List<string> ripFileNames = scanManager.GetAllRipsWithReleaseDate(scanRipsOpts.WithReleaseDate).ToList();

                System.Console.WriteLine($"Contagem: {ripFileNames.Count()}\n");

                foreach (var fileName in ripFileNames.OrderBy(r => r))
                {
                    System.Console.WriteLine(fileName);
                }
            }
            else if (scanRipsOpts.CountByVisit)
            {
                System.Console.WriteLine("Scan: contagem por visita\n");
                Dictionary<DateTime, int> countByVisit = scanManager.GetRipCountByVisit();
                foreach (var item in countByVisit.OrderBy(kvp => kvp.Key))
                {
                    string visitStr = item.Key.ToString("yyyyMMdd");
                    System.Console.WriteLine($"{visitStr} : {item.Value}");
                }
            }
            else
            {
                System.Console.WriteLine("Nada a fazer...");
            }
            System.Console.WriteLine();
        }

        public static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var errorObj in errors)
            {
                System.Console.WriteLine(errorObj.Tag);
            }
        }


    }
}

using CommandLine;

namespace FilmCRUD.Verbs
{
    // Exemplos:
    //  dotnet run -- visit --listcontents
    //  dotnet run -- visit --persistcontents 20220306
    [Verb("visit", HelpText = "Para gerar o txt com os conteúdos da warehouse e persistir no repo")]
    public class VisitOptions
    {
        [Option(SetName = "ListingContents", HelpText = "Gera o txt com os conteúdos da warehouse")]
        public bool ListContents { get; set; }

        [Option(SetName = "PersistingContents", HelpText = "Lê conteúdos da warehouse de um txt e persiste no repo")]
        public string PersistContents { get; set; }
    }

}
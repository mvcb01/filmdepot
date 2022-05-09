using CommandLine;

namespace FilmCRUD.Verbs
{
    // Exemplos:
    //  dotnet run -- visit --listcontents
    //  dotnet run -- visit --persistcontents --contentsdate 20220306
    [Verb("visit", HelpText = "Para gerar o txt com os conteúdos da warehouse e persistir no repo")]
    public class VisitOptions
    {
        [Option(SetName = "ListingContents", HelpText = "Gera o txt com os conteúdos da warehouse")]
        public bool ListContents { get; set; }

        [Option(SetName = "PersistingContents", HelpText = "Lê conteúdos da warehouse de um txt e persiste no repo")]
        public bool PersistContents { get; set; }

        [Option(SetName = "PersistingContents", HelpText = "Sufixo do txt a ler, por exemplo 20210923")]
        public string ContentsDate { get; set; }
    }
}
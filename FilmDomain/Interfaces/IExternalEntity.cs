namespace FilmDomain.Interfaces
{
    public interface IExternalEntity
    {
        int ExternalId { get; set; }

        string Name { get; set; }
    }
}
namespace FilmDomain.Interfaces
{
    public interface INamedEntityWithId
    {
        int Id { get; set; }

        string Name { get; set; }
    }
}
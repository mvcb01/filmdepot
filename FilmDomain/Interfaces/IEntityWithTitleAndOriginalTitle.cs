namespace FilmDomain.Interfaces
{
    public interface IEntityWithTitleAndOriginalTitle
    {
        public string Title { get; set; }

        public string OriginalTitle { get; set; }
    }
}

using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmDataAccess.EFCore.ModelConfiguration
{
    public class MovieEntityTypeConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.Property(m => m.Title).IsRequired();

            // a combinação (título, título original, release date) deve identificar bem um filme
            builder.HasAlternateKey(m => new { m.Title, m.OriginalTitle, m.ReleaseDate });

            builder.Navigation(m => m.Actors).AutoInclude();
            builder.Navigation(m => m.Directors).AutoInclude();
            builder.Navigation(m => m.Genres).AutoInclude();
            builder.Navigation(m => m.MovieRips).AutoInclude();
        }
    }
}
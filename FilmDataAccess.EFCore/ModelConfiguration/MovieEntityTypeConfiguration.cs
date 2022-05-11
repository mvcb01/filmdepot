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

            // a combinação (título, release date) deve identificar bem um filme
            builder.HasAlternateKey(m => new {m.Title, m.OriginalTitle, m.ReleaseDate });
        }
    }
}
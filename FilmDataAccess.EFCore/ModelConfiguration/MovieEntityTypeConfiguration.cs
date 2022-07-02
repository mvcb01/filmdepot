using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Collections.Generic;

namespace FilmDataAccess.EFCore.ModelConfiguration
{
    public class MovieEntityTypeConfiguration : IEntityTypeConfiguration<Movie>
    {
        public void Configure(EntityTypeBuilder<Movie> builder)
        {
            builder.Property(m => m.Title).IsRequired();
            builder.HasAlternateKey(m => m.ExternalId);

            // see:
            //  https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions?tabs=fluent-api
            builder
                .Property(m => m.Keywords)
                .HasConversion(
                    k => JsonSerializer.Serialize(k, (JsonSerializerOptions)null),
                    k => JsonSerializer.Deserialize<ICollection<string>>(k, (JsonSerializerOptions)null));

            builder.Navigation(m => m.Actors).AutoInclude();
            builder.Navigation(m => m.Directors).AutoInclude();
            builder.Navigation(m => m.Genres).AutoInclude();
            builder.Navigation(m => m.MovieRips).AutoInclude();

        }
    }
}
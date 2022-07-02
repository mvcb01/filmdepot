using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
            // builder
            //     .Property(m => m.Keywords)
            //     .HasConversion(
            //         k => JsonSerializer.Serialize(k, (JsonSerializerOptions)null),
            //         k => JsonSerializer.Deserialize<ICollection<string>>(k, (JsonSerializerOptions)null),
                    // new ValueComparer<ICollection<string>>(
                    //     (c1, c2) => c1.SequenceEqual(c2),
                    //     c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    //     c => (ICollection<string>)c.ToList()));

            builder.Property(m => m.Keywords).HasConversion(new ValueConverter<ICollection<string>, string>(
                k => JsonSerializer.Serialize(k, (JsonSerializerOptions)null),
                k => JsonSerializer.Deserialize<ICollection<string>>(k, (JsonSerializerOptions)null)
            ));


            builder.Navigation(m => m.Actors).AutoInclude();
            builder.Navigation(m => m.Directors).AutoInclude();
            builder.Navigation(m => m.Genres).AutoInclude();
            builder.Navigation(m => m.MovieRips).AutoInclude();
        }
    }
}
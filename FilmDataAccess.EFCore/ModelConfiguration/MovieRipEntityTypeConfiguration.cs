using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmDataAccess.EFCore.ModelConfiguration
{
    public class MovieRipEntityTypeConfiguration : IEntityTypeConfiguration<MovieRip>
    {
        public void Configure(EntityTypeBuilder<MovieRip> builder)
        {
            builder.Property(mr => mr.FileName).IsRequired();
            builder.HasAlternateKey(mr => mr.FileName);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmDataAccess.EFCore.ModelConfiguration
{
    public class DirectorEntityTypeConfiguration : IEntityTypeConfiguration<Director>
    {
        public void Configure(EntityTypeBuilder<Director> builder)
        {
            builder.Property(d => d.Name).IsRequired();
            builder.HasAlternateKey(d => d.Name);
        }
    }
}
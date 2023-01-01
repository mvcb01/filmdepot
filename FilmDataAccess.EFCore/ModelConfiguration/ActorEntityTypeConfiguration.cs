using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmDataAccess.EFCore.ModelConfiguration
{
    public class ActorEntityTypeConfiguration : IEntityTypeConfiguration<CastMember>
    {
        public void Configure(EntityTypeBuilder<CastMember> builder)
        {
            builder.Property(a => a.Name).IsRequired();
            builder.HasAlternateKey(a => a.ExternalId);

        }
    }
}
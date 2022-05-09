using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FilmDataAccess.EFCore.ModelConfiguration
{
    public class MovieWarehouseVisitEntityTypeConfiguration : IEntityTypeConfiguration<MovieWarehouseVisit>
    {
        public void Configure(EntityTypeBuilder<MovieWarehouseVisit> builder)
        {
            builder.Property(v => v.VisitDateTime).IsRequired();

            // os objectos MovieWarehouseVisit incluem sempre os MovieRips associados
            builder.Navigation(v => v.MovieRips).AutoInclude();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using FilmDomain.Entities;
using ConfigUtils;
using FilmDataAccess.EFCore.ModelConfiguration;

namespace FilmDataAccess.EFCore
{
    public class SQLiteAppContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; }

        public DbSet<Genre> Genres { get; set; }

        public DbSet<MovieRip> MovieRips { get; set; }

        public DbSet<MovieWarehouseVisit> MovieWarehouseVisits { get; set; }

        private static readonly AppSettingsManager _appSettingsManager = new("appsettings.json");

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(_appSettingsManager.GetConnectionString("FilmDb"));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration<Movie>(new MovieEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration<Genre>(new GenreEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration<MovieRip>(new MovieRipEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration<MovieWarehouseVisit>(new MovieWarehouseVisitEntityTypeConfiguration());
        }

    }
}
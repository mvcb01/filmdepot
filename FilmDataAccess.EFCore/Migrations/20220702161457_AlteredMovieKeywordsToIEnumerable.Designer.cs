﻿// <auto-generated />
using System;
using FilmDataAccess.EFCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FilmDataAccess.EFCore.Migrations
{
    [DbContext(typeof(SQLiteAppContext))]
    [Migration("20220702161457_AlteredMovieKeywordsToIEnumerable")]
    partial class AlteredMovieKeywordsToIEnumerable
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("ActorMovie", b =>
                {
                    b.Property<int>("ActorsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MoviesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("ActorsId", "MoviesId");

                    b.HasIndex("MoviesId");

                    b.ToTable("ActorMovie");
                });

            modelBuilder.Entity("DirectorMovie", b =>
                {
                    b.Property<int>("DirectorsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MoviesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("DirectorsId", "MoviesId");

                    b.HasIndex("MoviesId");

                    b.ToTable("DirectorMovie");
                });

            modelBuilder.Entity("FilmDomain.Entities.Actor", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasAlternateKey("ExternalId");

                    b.ToTable("Actors");
                });

            modelBuilder.Entity("FilmDomain.Entities.Director", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasAlternateKey("ExternalId");

                    b.ToTable("Directors");
                });

            modelBuilder.Entity("FilmDomain.Entities.Genre", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasAlternateKey("ExternalId");

                    b.ToTable("Genres");
                });

            modelBuilder.Entity("FilmDomain.Entities.Movie", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExternalId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("IMDBId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Keywords")
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalTitle")
                        .HasColumnType("TEXT");

                    b.Property<int>("ReleaseDate")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasAlternateKey("ExternalId");

                    b.ToTable("Movies");
                });

            modelBuilder.Entity("FilmDomain.Entities.MovieRip", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("MovieId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ParsedReleaseDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("ParsedRipGroup")
                        .HasColumnType("TEXT");

                    b.Property<string>("ParsedRipInfo")
                        .HasColumnType("TEXT");

                    b.Property<string>("ParsedRipQuality")
                        .HasColumnType("TEXT");

                    b.Property<string>("ParsedTitle")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasAlternateKey("FileName");

                    b.HasIndex("MovieId");

                    b.ToTable("MovieRips");
                });

            modelBuilder.Entity("FilmDomain.Entities.MovieWarehouseVisit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("VisitDateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("MovieWarehouseVisits");
                });

            modelBuilder.Entity("GenreMovie", b =>
                {
                    b.Property<int>("GenresId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MoviesId")
                        .HasColumnType("INTEGER");

                    b.HasKey("GenresId", "MoviesId");

                    b.HasIndex("MoviesId");

                    b.ToTable("GenreMovie");
                });

            modelBuilder.Entity("MovieRipMovieWarehouseVisit", b =>
                {
                    b.Property<int>("MovieRipsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MovieWarehouseVisitsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("MovieRipsId", "MovieWarehouseVisitsId");

                    b.HasIndex("MovieWarehouseVisitsId");

                    b.ToTable("MovieRipMovieWarehouseVisit");
                });

            modelBuilder.Entity("ActorMovie", b =>
                {
                    b.HasOne("FilmDomain.Entities.Actor", null)
                        .WithMany()
                        .HasForeignKey("ActorsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FilmDomain.Entities.Movie", null)
                        .WithMany()
                        .HasForeignKey("MoviesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("DirectorMovie", b =>
                {
                    b.HasOne("FilmDomain.Entities.Director", null)
                        .WithMany()
                        .HasForeignKey("DirectorsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FilmDomain.Entities.Movie", null)
                        .WithMany()
                        .HasForeignKey("MoviesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FilmDomain.Entities.MovieRip", b =>
                {
                    b.HasOne("FilmDomain.Entities.Movie", "Movie")
                        .WithMany("MovieRips")
                        .HasForeignKey("MovieId");

                    b.Navigation("Movie");
                });

            modelBuilder.Entity("GenreMovie", b =>
                {
                    b.HasOne("FilmDomain.Entities.Genre", null)
                        .WithMany()
                        .HasForeignKey("GenresId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FilmDomain.Entities.Movie", null)
                        .WithMany()
                        .HasForeignKey("MoviesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("MovieRipMovieWarehouseVisit", b =>
                {
                    b.HasOne("FilmDomain.Entities.MovieRip", null)
                        .WithMany()
                        .HasForeignKey("MovieRipsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("FilmDomain.Entities.MovieWarehouseVisit", null)
                        .WithMany()
                        .HasForeignKey("MovieWarehouseVisitsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("FilmDomain.Entities.Movie", b =>
                {
                    b.Navigation("MovieRips");
                });
#pragma warning restore 612, 618
        }
    }
}

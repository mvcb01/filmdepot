using Xunit;
using Moq;
using FluentAssertions;
using System.Linq.Expressions;
using System;
using System.Collections.Generic;

using FilmDomain.Entities;
using FilmDomain.Interfaces;
using MovieAPIClients.Interfaces;
using FilmCRUD;


namespace DepotTests.CRUDTests
{
    public class MovieDetailsFetcherTests
    {
        private readonly Mock<IMovieRepository> _movieRepositoryMock;

        private readonly Mock<IUnitOfWork> _unitOfWorkMock;

        private readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        private readonly MovieDetailsFetcher _movieDetailsFetcher;

        public MovieDetailsFetcherTests()
        {
            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);

            this._movieAPIClientMock = new Mock<IMovieAPIClient>(MockBehavior.Strict);

            this._movieDetailsFetcher = new MovieDetailsFetcher(
                this._unitOfWorkMock.Object,
                this._movieAPIClientMock.Object);
        }


    }
}
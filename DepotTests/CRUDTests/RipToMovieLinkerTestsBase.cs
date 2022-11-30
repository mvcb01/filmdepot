using ConfigUtils.Interfaces;
using FilmCRUD;
using FilmDomain.Interfaces;
using Moq;
using MovieAPIClients.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DepotTests.CRUDTests
{
    /// <summary>
    /// Class <c>RipToMovieLinkerTestsBase</c> sets the base Mock properties used in for derived test classes
    /// </summary>
    public class RipToMovieLinkerTestsBase
    {
        protected readonly Mock<IMovieRipRepository> _movieRipRepositoryMock;

        protected readonly Mock<IMovieRepository> _movieRepositoryMock;

        protected readonly Mock<IUnitOfWork> _unitOfWorkMock;

        protected readonly Mock<IMovieAPIClient> _movieAPIClientMock;

        protected readonly Mock<IRateLimitPolicyConfig> _rateLimitConfigMock;

        protected readonly Mock<IRetryPolicyConfig> _retryConfigMock;

        protected readonly Mock<IAppSettingsManager> _appSettingsManagerMock;

        protected readonly RipToMovieLinker _ripToMovieLinker;

        public RipToMovieLinkerTestsBase()
        {
            this._movieRipRepositoryMock = new Mock<IMovieRipRepository>(MockBehavior.Strict);
            this._movieRepositoryMock = new Mock<IMovieRepository>(MockBehavior.Strict);

            this._unitOfWorkMock = new Mock<IUnitOfWork>();
            this._unitOfWorkMock
                .SetupGet(u => u.MovieRips)
                .Returns(this._movieRipRepositoryMock.Object);
            this._unitOfWorkMock
                .SetupGet(u => u.Movies)
                .Returns(this._movieRepositoryMock.Object);

            this._movieAPIClientMock = new Mock<IMovieAPIClient>(MockBehavior.Strict);
            this._movieAPIClientMock.SetupGet(m => m.ApiBaseAddress).Returns("https://api.dummy.org/");

            this._appSettingsManagerMock = new Mock<IAppSettingsManager>();

            this._rateLimitConfigMock = new Mock<IRateLimitPolicyConfig>();
            this._retryConfigMock = new Mock<IRetryPolicyConfig>();

            // default policy configs
            this._rateLimitConfigMock.SetupGet(pol => pol.NumberOfExecutions).Returns(5);
            this._rateLimitConfigMock.SetupGet(pol => pol.PerTimeSpan).Returns(TimeSpan.FromMilliseconds(50));

            this._retryConfigMock.SetupGet(pol => pol.RetryCount).Returns(2);
            this._retryConfigMock.SetupGet(pol => pol.SleepDuration).Returns(TimeSpan.FromMilliseconds(50));

            this._appSettingsManagerMock.Setup(a => a.GetRateLimitPolicyConfig()).Returns(this._rateLimitConfigMock.Object);
            this._appSettingsManagerMock.Setup(a => a.GetRetryPolicyConfig()).Returns(this._retryConfigMock.Object);

            this._ripToMovieLinker = new RipToMovieLinker(
                this._unitOfWorkMock.Object,
                this._appSettingsManagerMock.Object,
                this._movieAPIClientMock.Object);
        }
    }
}

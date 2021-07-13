using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Gateway.Applications;
using SFA.DAS.ApplyService.Domain.Apply.Gateway;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.GetGatewayApplicationsCountsHandlerTests
{
    [TestFixture]
    public class GetGatewayApplicationsCountsHandlerTests
    {
        private GetGatewayApplicationsCountsHandler _handler;
        private Mock<IGatewayRepository> _repository;

        [SetUp]
        public void TestSetup()
        {
            var data = new List<GatewayApplicationStatusCount>
            {
                new GatewayApplicationStatusCount
                    {GatewayReviewStatus = GatewayReviewStatus.New, ApplicationStatus = ApplicationStatus.Submitted, Count = 1},
                new GatewayApplicationStatusCount
                    {GatewayReviewStatus = GatewayReviewStatus.InProgress, ApplicationStatus = ApplicationStatus.Submitted, Count = 2},
                new GatewayApplicationStatusCount
                    {GatewayReviewStatus = GatewayReviewStatus.ClarificationSent, ApplicationStatus = ApplicationStatus.Submitted, Count = 4},
                new GatewayApplicationStatusCount
                    {GatewayReviewStatus = GatewayReviewStatus.Fail, ApplicationStatus = ApplicationStatus.GatewayAssessed, Count = 8},
                new GatewayApplicationStatusCount
                    {GatewayReviewStatus = GatewayReviewStatus.Pass, ApplicationStatus = ApplicationStatus.GatewayAssessed, Count = 16},
                new GatewayApplicationStatusCount
                    {GatewayReviewStatus = GatewayReviewStatus.Rejected, ApplicationStatus = ApplicationStatus.GatewayAssessed, Count = 32}
            };

            _repository = new Mock<IGatewayRepository>();
            _repository.Setup(x => x.GetGatewayApplicationStatusCounts(It.IsAny<string>())).ReturnsAsync(data);

            _handler = new GetGatewayApplicationsCountsHandler(_repository.Object);
        }

        [Test]
        public async Task GetGatewayApplicationsCountsHandler_returns_correct_counts()
        {
            var result = await _handler.Handle(new GetGatewayApplicationCountsRequest(), new CancellationToken());

            Assert.AreEqual(1, result.NewApplicationsCount);
            Assert.AreEqual(6, result.InProgressApplicationsCount);
            Assert.AreEqual(56, result.ClosedApplicationsCount);
        }
    }
}

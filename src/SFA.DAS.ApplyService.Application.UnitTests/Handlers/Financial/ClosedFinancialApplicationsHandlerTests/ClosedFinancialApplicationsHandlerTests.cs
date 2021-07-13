using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Financial.Applications;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.Financial.ClosedFinancialApplicationsHandlerTests
{
    [TestFixture]
    public class ClosedFinancialApplicationsHandlerTests
    {
        private Mock<IApplyRepository> _applyRepository;
        private ClosedFinancialApplicationsHandler _handler;

        private List<RoatpFinancialSummaryItem> _closedApplications;

        [SetUp]
        public void Setup()
        {
            var closedFinancialApplication = new RoatpFinancialSummaryItem { ApplicationStatus = ApplicationStatus.GatewayAssessed, FinancialReviewStatus = FinancialReviewStatus.Pass, SubmittedDate = DateTime.Now, OutcomeMadeDate = DateTime.Now };

            _closedApplications = new List<RoatpFinancialSummaryItem> { closedFinancialApplication };

            _applyRepository = new Mock<IApplyRepository>();
            _applyRepository.Setup(r => r.GetClosedFinancialApplications(null,null)).ReturnsAsync(_closedApplications);

            _handler = new ClosedFinancialApplicationsHandler(_applyRepository.Object);
        }

        [Test]
        public async Task OClosedFinancialApplicationsHandler_returns_expected_applications()
        {
            var request = new ClosedFinancialApplicationsRequest(null,null);
            var result = await _handler.Handle(request, new CancellationToken());

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_closedApplications);
        }
    }
}

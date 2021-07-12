using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Financial.Applications;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.Financial.FinancialApplicationsStatusCountsHandlerTests
{
    [TestFixture]
    public class FinancialApplicationsStatusCountsHandlerTests
    {
        private Mock<IApplyRepository> _applyRepository;
        private FinancialApplicationsStatusCountsHandler _handler;

        private List<RoatpFinancialSummaryItem> _openApplications;
        private List<RoatpFinancialSummaryItem> _clarificationApplications;
        private List<RoatpFinancialSummaryItem> _closedApplications;

        [SetUp]
        public void Setup()
        {
            var openFinancialApplication = new RoatpFinancialSummaryItem { ApplicationStatus = ApplicationStatus.GatewayAssessed, FinancialReviewStatus = FinancialReviewStatus.New, DeclaredInApplication = "Not exempt",  SubmittedDate = DateTime.Now };
            var openExemptFinancialApplication = new RoatpFinancialSummaryItem { ApplicationStatus = ApplicationStatus.GatewayAssessed, FinancialReviewStatus = FinancialReviewStatus.New, DeclaredInApplication = "Exempt", SubmittedDate = DateTime.Now };
            var clarificationFinancialApplication = new RoatpFinancialSummaryItem { ApplicationStatus = ApplicationStatus.GatewayAssessed, FinancialReviewStatus = FinancialReviewStatus.ClarificationSent, SubmittedDate = DateTime.Now, ClarificationRequestedDate = DateTime.Now };
            var closedFinancialApplication = new RoatpFinancialSummaryItem { ApplicationStatus = ApplicationStatus.GatewayAssessed, FinancialReviewStatus = FinancialReviewStatus.Pass, SubmittedDate = DateTime.Now, OutcomeMadeDate = DateTime.Now };

            _openApplications = new List<RoatpFinancialSummaryItem> { openFinancialApplication, openExemptFinancialApplication };
            _clarificationApplications = new List<RoatpFinancialSummaryItem> { clarificationFinancialApplication };
            _closedApplications = new List<RoatpFinancialSummaryItem> { closedFinancialApplication };

            _applyRepository = new Mock<IApplyRepository>();

            _applyRepository.Setup(r => r.GetFinancialApplicationsStatusCounts(null)).ReturnsAsync(new RoatpFinancialApplicationsStatusCounts
            {
                ApplicationsOpen = _openApplications.Count,
                ApplicationsWithClarification = _clarificationApplications.Count,
                ApplicationsClosed = _closedApplications.Count
            });

            _handler = new FinancialApplicationsStatusCountsHandler(_applyRepository.Object);
        }

        [Test]
        public async Task StatusCounts_should_be_as_expected()
        {
            int expectedOpenCount = _openApplications.Count;
            int expectedClarificationCount = _clarificationApplications.Count;
            int expectedClosedCount = _closedApplications.Count;

            var request = new FinancialApplicationsStatusCountsRequest(null);
            var result = await _handler.Handle(request, new CancellationToken());

            result.Should().NotBeNull();
            result.ApplicationsOpen.Should().Be(expectedOpenCount);
            result.ApplicationsWithClarification.Should().Be(expectedClarificationCount);
            result.ApplicationsClosed.Should().Be(expectedClosedCount);
        }
    }
}

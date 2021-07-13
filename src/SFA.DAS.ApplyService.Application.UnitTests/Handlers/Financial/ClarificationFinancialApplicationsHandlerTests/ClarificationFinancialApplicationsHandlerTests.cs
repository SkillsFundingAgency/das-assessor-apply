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

namespace SFA.DAS.ApplyService.Application.UnitTests.Handlers.Financial.ClarificationFinancialApplicationsHandlerTests
{
    [TestFixture]
    public class ClarificationFinancialApplicationsHandlerTests
    {
        private Mock<IApplyRepository> _applyRepository;
        private ClarificationFinancialApplicationsHandler _handler;

        private List<RoatpFinancialSummaryItem> _clarificationApplications;

        [SetUp]
        public void Setup()
        {
            var clarificationFinancialApplication = new RoatpFinancialSummaryItem { ApplicationStatus = ApplicationStatus.GatewayAssessed, FinancialReviewStatus = FinancialReviewStatus.ClarificationSent, SubmittedDate = DateTime.Now, ClarificationRequestedDate = DateTime.Now };

            _clarificationApplications = new List<RoatpFinancialSummaryItem> { clarificationFinancialApplication };

            _applyRepository = new Mock<IApplyRepository>();
            _applyRepository.Setup(r => r.GetClarificationFinancialApplications(null,null)).ReturnsAsync(_clarificationApplications);

            _handler = new ClarificationFinancialApplicationsHandler(_applyRepository.Object);
        }

        [Test]
        public async Task ClarificationFinancialApplicationsHandler_returns_expected_applications()
        {
            var request = new ClarificationFinancialApplicationsRequest(null,null);
            var result = await _handler.Handle(request, new CancellationToken());

            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(_clarificationApplications);
        }
    }
}

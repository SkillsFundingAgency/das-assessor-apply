using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using SFA.DAS.ApplyService.Application.Apply.Assessor;
using SFA.DAS.ApplyService.Application.Apply.Roatp;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.InternalApi.Controllers;
using SFA.DAS.ApplyService.InternalApi.Infrastructure;
using SFA.DAS.ApplyService.InternalApi.Types.Assessor;
using SFA.DAS.ApplyService.InternalApi.Services.Assessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Application.Apply.GetApplications;
using SFA.DAS.ApplyService.InternalApi.Services.Moderator;

namespace SFA.DAS.ApplyService.InternalApi.UnitTests.Controllers
{
    [TestFixture]
    public class RoatpAssessorControllerTests
    {
        private readonly Guid _applicationId = Guid.NewGuid();
        private const int _sequenceNumber = RoatpWorkflowSequenceIds.DeliveringApprenticeshipTraining;
        private const int _sectionNumber = RoatpWorkflowSectionIds.DeliveringApprenticeshipTraining.ManagementHierarchy;
        private const string _pageId = RoatpWorkflowPageIds.DeliveringApprenticeshipTraining.ManagementHierarchy;
        private const string _userId = "userid";
        private const string _userName = "username";

        private Mock<IMediator> _mediator;
        private Mock<IInternalQnaApiClient> _qnaApiClient;
        private Mock<IAssessorSequenceService> _sequenceService;
        private Mock<IAssessorPageService> _pageService;
        private Mock<IAssessorSectorService> _sectorService;  
        private Mock<IAssessorSectorDetailsService> _sectorDetailsService;
        private Mock<IAssessorReviewCreationService> _assessorReviewCreationService;
        private Mock<IModeratorReviewCreationService> _moderatorReviewCreationService;

        private RoatpAssessorController _controller;


        [SetUp]
        public void TestSetup()
        {
            _mediator = new Mock<IMediator>();
            _qnaApiClient = new Mock<IInternalQnaApiClient>();
            _sequenceService = new Mock<IAssessorSequenceService>();
            _pageService = new Mock<IAssessorPageService>();
            _sectorService = new Mock<IAssessorSectorService>();
            _sectorDetailsService = new Mock<IAssessorSectorDetailsService>();
            _assessorReviewCreationService = new Mock<IAssessorReviewCreationService>();
            _moderatorReviewCreationService = new Mock<IModeratorReviewCreationService>();

            _controller = new RoatpAssessorController(_mediator.Object, _qnaApiClient.Object, _sequenceService.Object, _pageService.Object, _sectorService.Object, _sectorDetailsService.Object, _assessorReviewCreationService.Object, _moderatorReviewCreationService.Object);
        }

        [Test]
        public async Task Get_ApplicationCounts_returns_expected_counts_for_the_user()
        {
            var newApplications = 1;
            var inprogressApplications = 2;
            var moderationApplications = 3;
            var clarificationApplications = 4;
            var closedApplications = 5;

            var expectedResult = new AssessorApplicationCounts(newApplications, inprogressApplications, moderationApplications, clarificationApplications, closedApplications);
            _mediator.Setup(x => x.Send(It.Is<AssessorApplicationCountsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetApplicationCounts(_userId, null);

            _mediator.Verify(x => x.Send(It.Is<AssessorApplicationCountsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task Get_NewApplications_returns_new_applications_for_the_user()
        {
            var expectedResult = new List<AssessorApplicationSummary>();
            _mediator.Setup(x => x.Send(It.Is<NewAssessorApplicationsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.NewApplications(_userId, null, null, null);
            
            _mediator.Verify(x => x.Send(It.Is<NewAssessorApplicationsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task Get_InProgressApplications_returns_in_progress_applications_for_the_user()
        {
            var expectedResult = new List<AssessorApplicationSummary>();
            _mediator.Setup(x => x.Send(It.Is<InProgressAssessorApplicationsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.InProgressApplications(_userId, null, null, null);

            _mediator.Verify(x => x.Send(It.Is<InProgressAssessorApplicationsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task Get_InModerationApplications_returns_applications_for_the_user()
        {
            var expectedResult = new List<ModerationApplicationSummary>();
            _mediator.Setup(x => x.Send(It.Is<ApplicationsInModerationRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.InModerationApplications(_userId, null, null, null);

            _mediator.Verify(x => x.Send(It.Is<ApplicationsInModerationRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task Get_InClarificationApplications_returns_applications_for_the_user()
        {
            var expectedResult = new List<ClarificationApplicationSummary>();
            _mediator.Setup(x => x.Send(It.Is<ApplicationsInClarificationRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.InClarificationApplications(_userId, null, null, null);

            _mediator.Verify(x => x.Send(It.Is<ApplicationsInClarificationRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task Get_ClosedApplications_returns_applications_for_the_user()
        {
            var expectedResult = new List<ClosedApplicationSummary>();
            _mediator.Setup(x => x.Send(It.Is<ClosedAssessorApplicationsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.ClosedApplications(_userId, null, null, null);

            _mediator.Verify(x => x.Send(It.Is<ClosedAssessorApplicationsRequest>(y => y.UserId == _userId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }


        [Test]
        public async Task AssignApplication_sets_assessor_details()
        {
            var request = new RoatpAssessorController.AssignAssessorCommand { AssessorName = "sdfjfsdg", AssessorNumber = 1, AssessorUserId = _userId };
            var applicationid = Guid.NewGuid();
            _mediator.Setup(x => x.Send(It.IsAny<GetApplicationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Apply());

            await _controller.AssignApplication(applicationid, request);

            _mediator.Verify(x => x.Send(It.Is<AssignAssessorRequest>(r => r.ApplicationId == applicationid && r.AssessorName == request.AssessorName && r.AssessorNumber == request.AssessorNumber && r.AssessorUserId == request.AssessorUserId), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task AssignApplication_creates_review_outcomes_first_time()
        {
            var request = new RoatpAssessorController.AssignAssessorCommand { AssessorName = "sdfjfsdg", AssessorNumber = 1, AssessorUserId = _userId };
            var applicationid = Guid.NewGuid();
            _mediator.Setup(x => x.Send(It.IsAny<GetApplicationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Apply());

            await _controller.AssignApplication(applicationid, request);

            _assessorReviewCreationService.Verify(x => x.CreateEmptyReview(It.Is<Guid>(r => r == applicationid),
                                                           It.Is<string>(u => u == request.AssessorUserId),
                                                           It.Is<string>(u => u == request.AssessorName),
                                                           It.Is<int>(n => n == request.AssessorNumber)), Times.Once());
        }

        [Test]
        public async Task AssignApplication_does_not_create_review_outcomes_second_time()
        {
            var request = new RoatpAssessorController.AssignAssessorCommand { AssessorName = "sdfjfsdg", AssessorNumber = 1, AssessorUserId = _userId };
            var applicationid = Guid.NewGuid();
            _mediator.Setup(x => x.Send(It.IsAny<GetApplicationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Apply{ Assessor1ReviewStatus = AssessorReviewStatus.InProgress});

            await _controller.AssignApplication(applicationid, request);

            _assessorReviewCreationService.Verify(x => x.CreateEmptyReview(It.IsAny<Guid>(),It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>()), Times.Never());
        }

        [Test]
        public async Task GetAssessorOverview_gets_expected_sequences()
        {
            var expectedResult = new List<AssessorSequence>();
            _sequenceService.Setup(x => x.GetSequences(_applicationId)).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetAssessorOverview(_applicationId);

            _sequenceService.Verify(x => x.GetSequences(_applicationId), Times.Once);
            CollectionAssert.AreEqual(expectedResult, actualResult);
        }

        [Test]
        public async Task SubmitPageReviewOutcome_calls_mediator()
        {
            var request = new RoatpAssessorController.SubmitPageReviewOutcomeCommand { SequenceNumber = _sequenceNumber, SectionNumber = _sectionNumber, PageId = _pageId, UserId = _userId, UserName = _userName, Status = "Fail", Comment = "Very bad" };

            await _controller.SubmitPageReviewOutcome(_applicationId, request);

            _mediator.Verify(x => x.Send(It.Is<SubmitAssessorPageOutcomeRequest>(r => r.ApplicationId == _applicationId && r.SequenceNumber == request.SequenceNumber && r.SectionNumber == request.SectionNumber && 
                   r.PageId == request.PageId && r.UserId == request.UserId && r.UserName == request.UserName && r.Status == request.Status && r.Comment == request.Comment), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetPageReviewOutcome_returns_expected_PageReviewOutcome()
        {
            var request = new RoatpAssessorController.GetPageReviewOutcomeRequest { SequenceNumber = _sequenceNumber, SectionNumber = _sectionNumber, PageId = _pageId, UserId = _userId } ;

            var expectedResult = new AssessorPageReviewOutcome();
            _mediator.Setup(x => x.Send(It.Is<GetAssessorPageReviewOutcomeRequest>(r => r.ApplicationId == _applicationId && r.SequenceNumber == request.SequenceNumber && r.SectionNumber == request.SectionNumber &&
                   r.PageId == request.PageId && r.UserId == request.UserId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetPageReviewOutcome(_applicationId, request);

            _mediator.Verify(x => x.Send(It.Is<GetAssessorPageReviewOutcomeRequest>(r => r.ApplicationId == _applicationId && r.SequenceNumber == request.SequenceNumber && r.SectionNumber == request.SectionNumber &&
                    r.PageId == request.PageId && r.UserId == request.UserId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task GetPageReviewOutcomesForSection_returns_expected_list_of_PageReviewOutcome()
        {
            var request = new RoatpAssessorController.GetPageReviewOutcomesForSectionRequest { SequenceNumber = _sequenceNumber, SectionNumber = _sectionNumber, UserId = _userId };

            var expectedResult = new List<AssessorPageReviewOutcome>();
            _mediator.Setup(x => x.Send(It.Is<GetAssessorPageReviewOutcomesForSectionRequest>(r => r.ApplicationId == _applicationId && r.SequenceNumber == request.SequenceNumber && r.SectionNumber == request.SectionNumber &&
                   r.UserId == request.UserId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetPageReviewOutcomesForSection(_applicationId, request);

            _mediator.Verify(x => x.Send(It.Is<GetAssessorPageReviewOutcomesForSectionRequest>(r => r.ApplicationId == _applicationId && r.SequenceNumber == request.SequenceNumber && r.SectionNumber == request.SectionNumber &&
                   r.UserId == request.UserId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task GetAllPageReviewOutcomes_returns_expected_list_of_PageReviewOutcome()
        {
            var request = new RoatpAssessorController.GetAllPageReviewOutcomesRequest { UserId = _userId };

            var expectedResult = new List<AssessorPageReviewOutcome>();
            _mediator.Setup(x => x.Send(It.Is<GetAllAssessorPageReviewOutcomesRequest>(r => r.ApplicationId == _applicationId && 
                        r.UserId == request.UserId), It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetAllPageReviewOutcomes(_applicationId, request);

            _mediator.Verify(x => x.Send(It.Is<GetAllAssessorPageReviewOutcomesRequest>(r => r.ApplicationId == _applicationId &&
                        r.UserId == request.UserId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task GetFirstAssessorPage_returns_expected_page()
        {
            var expectedResult = new AssessorPage();
            _pageService.Setup(x => x.GetPage(_applicationId, _sequenceNumber, _sectionNumber, null)).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetFirstAssessorPage(_applicationId, _sequenceNumber, _sectionNumber);

            _pageService.Verify(x => x.GetPage(_applicationId, _sequenceNumber, _sectionNumber, null), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task GetAssessorPage_returns_expected_page()
        {
            var expectedResult = new AssessorPage();
            _pageService.Setup(x => x.GetPage(_applicationId, _sequenceNumber, _sectionNumber, _pageId)).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetAssessorPage(_applicationId, _sequenceNumber, _sectionNumber, _pageId);

            _pageService.Verify(x => x.GetPage(_applicationId, _sequenceNumber, _sectionNumber, _pageId), Times.Once);
            Assert.AreSame(expectedResult, actualResult);
        }

        [Test]
        public async Task GetSectors_returns_expected_list_of_sectors()
        {
            var request = new RoatpAssessorController.GetSectorsRequest { UserId = _userId };

            var expectedResult = new List<AssessorSector>();
            _sectorService.Setup(x => x.GetSectorsForAssessor(_applicationId, _userId)).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetSectors(_applicationId, request);

            _sectorService.Verify(x => x.GetSectorsForAssessor(_applicationId, _userId), Times.Once);
            CollectionAssert.AreEqual(actualResult, expectedResult);
        }

        [Test]
        public async Task GetSectorDetails_returns_expected_sectordetails()
        {
            var expectedResult = new AssessorSectorDetails();
            _sectorDetailsService.Setup(x => x.GetSectorDetails(_applicationId, _pageId)).ReturnsAsync(expectedResult);

            var actualResult = await _controller.GetSectorDetails(_applicationId, _pageId);

            _sectorDetailsService.Verify(x => x.GetSectorDetails(_applicationId, _pageId), Times.Once);
            Assert.AreSame(actualResult, expectedResult);
        }

        [Test]
        public async Task DownloadFile_gets_expected_file()
        {
            var questionId = "1";
            var filename = "file.txt";

            var expectedFileStream = new FileStreamResult(new MemoryStream(), "application/pdf");
            _qnaApiClient.Setup(x => x.DownloadSpecifiedFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, questionId, filename)).ReturnsAsync(expectedFileStream);

            var actualResult = await _controller.DownloadFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, questionId, filename);

            _qnaApiClient.Verify(x => x.DownloadSpecifiedFile(_applicationId, _sequenceNumber, _sectionNumber, _pageId, questionId, filename), Times.Once);
            Assert.AreSame(expectedFileStream, actualResult);
        }

        [Test]
        public async Task UpdateAssessorReviewStatus_calls_mediator()
        {
            _mediator.Setup(x => x.Send(It.IsAny<GetApplicationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Apply());
            
            var request = new RoatpAssessorController.UpdateAssessorReviewStatusCommand { UserId = _userId, Status = AssessorReviewStatus.Approved };

            await _controller.UpdateAssessorReviewStatus(_applicationId, request);

            _mediator.Verify(x => x.Send(It.Is<UpdateAssessorReviewStatusRequest>(r => r.ApplicationId == _applicationId && r.UserId == request.UserId && r.Status == request.Status), It.IsAny<CancellationToken>()), Times.Once);
        }

        [TestCase(AssessorReviewStatus.Approved, AssessorReviewStatus.Approved, true)]
        [TestCase(AssessorReviewStatus.Declined, AssessorReviewStatus.Declined, false)]
        [TestCase(AssessorReviewStatus.Approved, AssessorReviewStatus.Draft, false)]
        [TestCase(AssessorReviewStatus.Approved , AssessorReviewStatus.InProgress, false)]
        [TestCase(AssessorReviewStatus.Approved, AssessorReviewStatus.Declined, false)]
        [TestCase(AssessorReviewStatus.Draft, AssessorReviewStatus.Approved, false)]
        [TestCase(AssessorReviewStatus.InProgress, AssessorReviewStatus.Approved, false)]
        [TestCase(AssessorReviewStatus.Declined, AssessorReviewStatus.Approved, false)]
        public async Task UpdateAssessorReviewStatus_creates_moderator_review_outcomes_when_both_assessors_have_approved(string assessor1ReviewStatus, string assessor2ReviewStatus, bool expectModeratorReviewCreation)
        {
            _mediator.Setup(x => x.Send(It.IsAny<GetApplicationRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new Apply
                {
                    Assessor1ReviewStatus = assessor1ReviewStatus, Assessor2ReviewStatus = assessor2ReviewStatus
                });

            var request = new RoatpAssessorController.UpdateAssessorReviewStatusCommand { UserId = _userId, UserName = _userName, Status = AssessorReviewStatus.Approved };

            await _controller.UpdateAssessorReviewStatus(_applicationId, request);

            _moderatorReviewCreationService.Verify(x => x.CreateEmptyReview(_applicationId, _userId, _userName), Times.Exactly(expectModeratorReviewCreation ? 1 : 0));
        }
    }
}

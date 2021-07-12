using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.ApplyService.Application.Apply.Assessor;
using SFA.DAS.ApplyService.Application.Apply.GetApplications;
using SFA.DAS.ApplyService.InternalApi.Infrastructure;
using SFA.DAS.ApplyService.InternalApi.Types.Assessor;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.InternalApi.Services.Assessor;
using SFA.DAS.ApplyService.InternalApi.Services.Moderator;
using SFA.DAS.ApplyService.Domain.Entities;

namespace SFA.DAS.ApplyService.InternalApi.Controllers
{
    [Authorize]
    public class RoatpAssessorController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IInternalQnaApiClient _qnaApiClient;
        private readonly IAssessorSequenceService _sequenceService;
        private readonly IAssessorPageService _pageService;
        private readonly IAssessorSectorService _sectorService;
        private readonly IAssessorSectorDetailsService _sectorDetailsService;
        private readonly IAssessorReviewCreationService _assessorReviewCreationService;
        private readonly IModeratorReviewCreationService _moderatorReviewCreationService;

        public RoatpAssessorController(IMediator mediator, IInternalQnaApiClient qnaApiClient,
            IAssessorSequenceService assessorSequenceService, IAssessorPageService assessorPageService,
            IAssessorSectorService assessorSectorService, IAssessorSectorDetailsService assessorSectorDetailsService,
            IAssessorReviewCreationService assessorReviewCreationService, IModeratorReviewCreationService moderatorReviewCreationService)
        {
            _mediator = mediator;
            _qnaApiClient = qnaApiClient;
            _sequenceService = assessorSequenceService;
            _pageService = assessorPageService;
            _sectorService = assessorSectorService;
            _sectorDetailsService = assessorSectorDetailsService;
            _assessorReviewCreationService = assessorReviewCreationService;
            _moderatorReviewCreationService = moderatorReviewCreationService;
        }

        [HttpGet("Assessor/Applications/{userId}")]
        public async Task<AssessorApplicationCounts> GetApplicationCounts(string userId, string searchTerm)
        {
            var summary = await _mediator.Send(new AssessorApplicationCountsRequest(userId, searchTerm));

            return summary;
        }

        [HttpGet("Assessor/Applications/{userId}/New")]
        public async Task<List<AssessorApplicationSummary>> NewApplications(string userId, string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new NewAssessorApplicationsRequest(userId, searchTerm, sortColumn, sortOrder));

            return applications;
        }

        [HttpGet("Assessor/Applications/{userId}/InProgress")]
        public async Task<List<AssessorApplicationSummary>> InProgressApplications(string userId, string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new InProgressAssessorApplicationsRequest(userId, searchTerm, sortColumn, sortOrder));

            return applications;
        }

        [HttpGet("Assessor/Applications/{userId}/InModeration")]
        public async Task<List<ModerationApplicationSummary>> InModerationApplications(string userId, string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new ApplicationsInModerationRequest(userId, searchTerm, sortColumn, sortOrder));

            return applications;
        }

        [HttpGet("Assessor/Applications/{userId}/InClarification")]
        public async Task<List<ClarificationApplicationSummary>> InClarificationApplications(string userId, string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new ApplicationsInClarificationRequest(userId, searchTerm, sortColumn, sortOrder));

            return applications;
        }

        [HttpGet("Assessor/Applications/{userId}/Closed")]
        public async Task<List<ClosedApplicationSummary>> ClosedApplications(string userId, string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new ClosedAssessorApplicationsRequest(userId, searchTerm, sortColumn, sortOrder));

            return applications;
        }


        [HttpPost("Assessor/Applications/{applicationId}/Assign")]
        public async Task AssignApplication(Guid applicationId, [FromBody] AssignAssessorCommand request)
        {
            var application = await _mediator.Send(new GetApplicationRequest(applicationId));

            await _mediator.Send(new AssignAssessorRequest(applicationId, request.AssessorNumber,
                request.AssessorUserId, request.AssessorName));

            if (string.IsNullOrWhiteSpace(application.Assessor1ReviewStatus) &&
                string.IsNullOrWhiteSpace(application.Assessor2ReviewStatus))
            {
                await _assessorReviewCreationService.CreateEmptyReview(applicationId, request.AssessorUserId, request.AssessorName, request.AssessorNumber);
            }
        }

        [HttpGet("Assessor/Applications/{applicationId}/Overview")]
        public async Task<List<AssessorSequence>> GetAssessorOverview(Guid applicationId)
        {
            var overviewSequences = await _sequenceService.GetSequences(applicationId);

            return overviewSequences.OrderBy(seq => seq.SequenceNumber).ToList();
        }

        [HttpPost("Assessor/Applications/{applicationId}/Sectors")]
        public async Task<List<AssessorSector>> GetSectors(Guid applicationId, [FromBody] GetSectorsRequest request)
        {
            var sectors = await _sectorService.GetSectorsForAssessor(applicationId, request.UserId);

            return sectors.OrderBy(sec => sec.PageId).ToList();
        }

        [HttpGet("Assessor/Applications/{applicationId}/SectorDetails/{pageId}")]
        public async Task<AssessorSectorDetails> GetSectorDetails(Guid applicationId, string pageId)
        {
            return await _sectorDetailsService.GetSectorDetails(applicationId, pageId);
        }

        [HttpGet("Assessor/Applications/{applicationId}/Sequences/{sequenceNumber}/Sections/{sectionNumber}/Page")]
        public async Task<AssessorPage> GetFirstAssessorPage(Guid applicationId, int sequenceNumber, int sectionNumber)
        {
            return await GetAssessorPage(applicationId, sequenceNumber, sectionNumber, null);
        }

        [HttpGet("Assessor/Applications/{applicationId}/Sequences/{sequenceNumber}/Sections/{sectionNumber}/Page/{pageId}")]
        public async Task<AssessorPage> GetAssessorPage(Guid applicationId, int sequenceNumber, int sectionNumber, string pageId)
        {
            return await _pageService.GetPage(applicationId, sequenceNumber, sectionNumber, pageId);
        }

        [HttpGet("Assessor/Applications/{applicationId}/Sequences/{sequenceNumber}/Sections/{sectionNumber}/Page/{pageId}/questions/{questionId}/download/{filename}")]
        public async Task<FileStreamResult> DownloadFile(Guid applicationId, int sequenceNumber, int sectionNumber,
            string pageId, string questionId, string filename)
        {
            return await _qnaApiClient.DownloadSpecifiedFile(applicationId, sequenceNumber, sectionNumber, pageId, questionId, filename);
        }


        [HttpPost("Assessor/Applications/{applicationId}/SubmitPageReviewOutcome")]
        public async Task SubmitPageReviewOutcome(Guid applicationId, [FromBody] SubmitPageReviewOutcomeCommand request)
        {
            await _mediator.Send(new SubmitAssessorPageOutcomeRequest(applicationId, request.SequenceNumber, request.SectionNumber, request.PageId, request.UserId, request.UserName, request.Status, request.Comment));
        }

        [HttpPost("Assessor/Applications/{applicationId}/GetPageReviewOutcome")]
        public async Task<AssessorPageReviewOutcome> GetPageReviewOutcome(Guid applicationId, [FromBody] GetPageReviewOutcomeRequest request)
        {
            var pageReviewOutcome = await _mediator.Send(new GetAssessorPageReviewOutcomeRequest(applicationId, request.SequenceNumber, request.SectionNumber, request.PageId, request.UserId));

            return pageReviewOutcome;
        }

        [HttpPost("Assessor/Applications/{applicationId}/GetPageReviewOutcomesForSection")]
        public async Task<List<AssessorPageReviewOutcome>> GetPageReviewOutcomesForSection(Guid applicationId, [FromBody] GetPageReviewOutcomesForSectionRequest request)
        {
            var assessorReviewOutcomes = await _mediator.Send(new GetAssessorPageReviewOutcomesForSectionRequest(applicationId, request.SequenceNumber, request.SectionNumber, request.UserId));

            return assessorReviewOutcomes;
        }

        [HttpPost("Assessor/Applications/{applicationId}/GetAllPageReviewOutcomes")]
        public async Task<List<AssessorPageReviewOutcome>> GetAllPageReviewOutcomes(Guid applicationId, [FromBody] GetAllPageReviewOutcomesRequest request)
        {
            var assessorReviewOutcomes = await _mediator.Send(new GetAllAssessorPageReviewOutcomesRequest(applicationId, request.UserId));

            return assessorReviewOutcomes;
        }

        [HttpPost("Assessor/Applications/{applicationId}/UpdateAssessorReviewStatus")]
        public async Task UpdateAssessorReviewStatus(Guid applicationId, [FromBody] UpdateAssessorReviewStatusCommand request)
        {
            await _mediator.Send(new UpdateAssessorReviewStatusRequest(applicationId, request.UserId, request.Status));

            var application = await _mediator.Send(new GetApplicationRequest(applicationId));

            if(application.Assessor1ReviewStatus == AssessorReviewStatus.Approved && application.Assessor2ReviewStatus == AssessorReviewStatus.Approved)
            { 
                await _moderatorReviewCreationService.CreateEmptyReview(applicationId, request.UserId, request.UserName);
            }
        }


        public class AssignAssessorCommand
        {
            public int AssessorNumber { get; set; }
            public string AssessorUserId { get; set; }
            public string AssessorName { get; set; }
        }

        public class SubmitPageReviewOutcomeCommand
        {
            public int SequenceNumber { get; set; }
            public int SectionNumber { get; set; }
            public string PageId { get; set; }
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Status { get; set; }
            public string Comment { get; set; }
        }

        public class GetPageReviewOutcomeRequest
        {
            public int SequenceNumber { get; set; }
            public int SectionNumber { get; set; }
            public string PageId { get; set; }
            public string UserId { get; set; }
        }

        public class GetPageReviewOutcomesForSectionRequest
        {
            public int SequenceNumber { get; set; }
            public int SectionNumber { get; set; }
            public string UserId { get; set; }
        }

        public class GetAllPageReviewOutcomesRequest
        {
            public string UserId { get; set; }
        }

        public class UpdateAssessorReviewStatusCommand
        {
            public string UserId { get; set; }
            public string UserName { get; set; }
            public string Status { get; set; }
        }

        public class GetSectorsRequest
        {
            public string UserId { get; set; }
        }
    }
}
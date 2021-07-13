using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.ApplyService.Application.Apply.Oversight;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Commands.CreateAppeal;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Commands.UploadAppealFile;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetAppeal;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetAppealUpload;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetOversightDetails;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetOversightReview;
using SFA.DAS.ApplyService.Application.Apply.Oversight.Queries.GetStagedFiles;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.QueryResults;
using SFA.DAS.ApplyService.InternalApi.Extensions;
using SFA.DAS.ApplyService.InternalApi.Services;
using SFA.DAS.ApplyService.InternalApi.Types.Requests.Oversight;
using SFA.DAS.ApplyService.InternalApi.Types.Responses.Oversight;

namespace SFA.DAS.ApplyService.InternalApi.Controllers
{
    [Authorize]
    public class OversightController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IRegistrationDetailsService _registrationDetailsService;

        public OversightController(IMediator mediator, IRegistrationDetailsService registrationDetailsService)
        {
            _mediator = mediator;
            _registrationDetailsService = registrationDetailsService;
        }

        [HttpGet]
        [Route("Oversights/Pending")]
        public async Task<ActionResult<PendingOversightReviews>> OversightsPending(string sortColumn, string sortOrder)
        {
            return await _mediator.Send(new GetOversightsPendingRequest(sortColumn,sortOrder));
        }

        [HttpGet]
        [Route("Oversights/Completed")]
        public async Task<ActionResult<CompletedOversightReviews>> OversightsCompleted(string sortColumn, string sortOrder)
        {
            return await _mediator.Send(new GetOversightsCompletedRequest(sortColumn, sortOrder));
        }

        [HttpGet]
        [Route("Oversights/Download")]
        public async Task<ActionResult<List<ApplicationOversightDownloadDetails>>> OversightDownload(DateTime dateFrom, DateTime dateTo)
        {
            return await _mediator.Send(new GetOversightDownloadRequest{ DateFrom = dateFrom, DateTo = dateTo });
        }

        [HttpPost]
        [Route("Oversight/Outcome")]
        public async Task<ActionResult<bool>> RecordOversightOutcome([FromBody] RecordOversightOutcomeCommand command)
        {
            return await _mediator.Send(command);
        }

        [HttpPost]
        [Route("Oversight/GatewayFailOutcome")]
        public async Task<ActionResult> RecordOversightGatewayFailOutcome([FromBody] RecordOversightGatewayFailOutcomeCommand command)
        {
            await _mediator.Send(command);
            return new OkResult();
        }

        [HttpPost]
        [Route("Oversight/GatewayRemovedOutcome")]
        public async Task<ActionResult> RecordOversightGatewayRemovedOutcome([FromBody] RecordOversightGatewayRemovedOutcomeCommand command)
        {
            await _mediator.Send(command);
            return new OkResult();
        }

        [HttpGet]
        [Route("Oversight/RegistrationDetails/{applicationId}")]
        public async Task<ActionResult<RoatpRegistrationDetails>> GetRegistrationDetails(Guid applicationId)
        {
            return await _registrationDetailsService.GetRegistrationDetails(applicationId);
        }
        
        [HttpGet]
        [Route("Oversights/{applicationId}")]
        public async Task<ActionResult<ApplicationOversightDetails>> OversightDetails(Guid applicationId)
        {
            return await _mediator.Send(new GetOversightApplicationDetailsRequest(applicationId));
        }

        [HttpGet]
        [Route("Oversight/{applicationId}/review")]
        public async Task<ActionResult<GetOversightReviewResponse>> OversightReview(GetOversightReviewRequest request)
        {
            var query = new GetOversightReviewQuery {ApplicationId = request.ApplicationId};

            var result = await _mediator.Send(query);

            return result == null ? null : new GetOversightReviewResponse
            {
                Id = result.Id,
                Status = result.Status,
                ApplicationDeterminedDate = result.ApplicationDeterminedDate,
                InProgressDate = result.InProgressDate,
                InProgressUserId = result.InProgressUserId,
                InProgressUserName = result.InProgressUserName,
                InProgressInternalComments = result.InProgressInternalComments,
                InProgressExternalComments = result.InProgressExternalComments,
                GatewayApproved = result.GatewayApproved,
                ModerationApproved = result.ModerationApproved,
                InternalComments = result.InternalComments,
                ExternalComments = result.ExternalComments,
                UserId = result.UserId,
                UserName = result.UserName
            };
        }

        [HttpPost]
        [Route("Oversight/{applicationId}/uploads")]
        public async Task<IActionResult> UploadAppealFile([FromForm] UploadAppealFileRequest request)
        {
            var command = new UploadAppealFileCommand
            {
                ApplicationId = request.ApplicationId,
                File = await request.File.ToFileUpload(),
                UserId = request.UserId,
                UserName = request.UserName
            };

            await _mediator.Send(command);
            return new OkResult();
        }

        [HttpPost]
        [Route("Oversight/{applicationId}/uploads/{fileId}/remove")]
        public async Task<IActionResult> RemoveAppealFile(Guid applicationId, Guid fileId, [FromBody] RemoveAppealFileRequest request)
        {
            var command = new RemoveAppealFileCommand
            {
                ApplicationId = applicationId,
                FileId = fileId,
                UserId = request.UserId,
                UserName = request.UserName
            };

            await _mediator.Send(command);
            return new OkResult();
        }
        
        [HttpGet]
        [Route("Oversight/{applicationId}/uploads")]
        public async Task<ActionResult<GetStagedFilesResponse>> StagedUploads([FromRoute] GetStagedFilesRequest request)
        {
            var query = new GetStagedFilesQuery
            {
                ApplicationId = request.ApplicationId
            };

            var result = await _mediator.Send(query);

            return new GetStagedFilesResponse
            {
                Files = result.Files.Select(file => new GetStagedFilesResponse.AppealFile{ Id = file.Id, Filename = file.Filename}).ToList()
            };
        }

        [HttpPost]
        [Route("Oversight/{applicationId}/oversight-reviews/{oversightReviewId}/appeal")]
        public async Task<IActionResult> CreateAppeal(Guid applicationId, Guid oversightReviewId, [FromBody] CreateAppealRequest request)
        {
            var command = new CreateAppealCommand
            {
                ApplicationId = applicationId,
                OversightReviewId = oversightReviewId,
                Message = request.Message,
                UserId = request.UserId,
                UserName = request.UserName
            };

            await _mediator.Send(command);
            return new OkResult();
        }

        [HttpGet]
        [Route("Oversight/{applicationId}/oversight-reviews/{oversightReviewId}/appeal")]
        public async Task<ActionResult<GetAppealResponse>> GetAppeal([FromRoute] GetAppealRequest request)
        {
            var query = new GetAppealQuery
            {
                ApplicationId = request.ApplicationId,
                OversightReviewId = request.OversightReviewId
            };

            var result = await _mediator.Send(query);

            return result == null ? null : new GetAppealResponse
            {
                Id = result.Id,
                Message = result.Message,
                CreatedOn = result.CreatedOn,
                UserId = result.UserId,
                UserName = result.UserName,
                Uploads = result.Uploads.Select(upload => new GetAppealResponse.AppealUpload
                {
                    Id = upload.Id,
                    Filename = upload.Filename,
                    ContentType = upload.ContentType
                }).ToList()
            };
        }

        [HttpGet]
        [Route("Oversight/{applicationId}/appeals/{appealId}/uploads/{appealUploadId}")]
        public async Task<ActionResult<GetAppealUploadResponse>> GetAppealUpload([FromRoute] GetAppealUploadRequest request)
        {
            var query = new GetAppealUploadQuery
            {
                ApplicationId = request.ApplicationId,
                AppealId = request.AppealId,
                AppealUploadId = request.AppealUploadId,
            };

            var result = await _mediator.Send(query);

            return new GetAppealUploadResponse
            {
                Filename = result.Filename,
                ContentType = result.ContentType,
                Content = result.Content
            };
        }
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Application.Apply.Financial;
using SFA.DAS.ApplyService.Application.Apply.Financial.Applications;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.InternalApi.Infrastructure;
using SFA.DAS.ApplyService.InternalApi.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Application.Apply.Roatp;

namespace SFA.DAS.ApplyService.InternalApi.Controllers
{
    [Authorize]
    public class FinancialController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IInternalQnaApiClient _qnaApiClient;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FinancialController> _logger;


        public FinancialController(IMediator mediator, IInternalQnaApiClient qnaApiClient, IFileStorageService fileStorageService, ILogger<FinancialController> logger)
        {
            _mediator = mediator;
            _qnaApiClient = qnaApiClient;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        [HttpGet("/Financial/OpenApplications")]
        public async Task<ActionResult> OpenApplications(string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new OpenFinancialApplicationsRequest(searchTerm, sortColumn, sortOrder));
            return Ok(applications);
        }

        [HttpGet("/Financial/OpenApplicationsForDownload")]
        public async Task<ActionResult> OpenApplicationsForDownload()
        {
            var applications = await _mediator.Send(new OpenFinancialApplicationsForDownloadRequest());
            return Ok(applications);
        }

        [HttpGet("/Financial/ClarificationApplications")]
        public async Task<ActionResult> ClarificationApplications(string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new ClarificationFinancialApplicationsRequest(searchTerm, sortColumn, sortOrder));
            return Ok(applications);
        }

        [HttpGet("/Financial/ClosedApplications")]
        public async Task<ActionResult> ClosedApplications(string searchTerm, string sortColumn, string sortOrder)
        {
            var applications = await _mediator.Send(new ClosedFinancialApplicationsRequest(searchTerm, sortColumn, sortOrder));
            return Ok(applications);
        }

        [HttpGet("/Financial/StatusCounts")]
        public async Task<ActionResult> StatusCounts(string searchTerm)
        {
            var statusCounts = await _mediator.Send(new FinancialApplicationsStatusCountsRequest(searchTerm));
            return Ok(statusCounts);
        }

        [HttpPost("/Financial/{applicationId}/Grade")]
        public async Task<IActionResult> RecordGrade(Guid applicationId, [FromBody] FinancialReviewDetails financialReviewDetails)
        {
            if(financialReviewDetails != null)
            {
                // Note: This gets around the WAF block
                financialReviewDetails.FinancialEvidences = await GetFinancialEvidence(applicationId);
            }

            await _mediator.Send(new RecordGradeRequest(applicationId, financialReviewDetails));
            return Ok();
        }


        [HttpPost("Clarification/Applications/{applicationId}/Upload")]
        public async Task<IActionResult> UploadClarificationFile(Guid applicationId, [FromForm] UploadClarificationFileCommand command)
        {
            if (Request.Form.Files != null && Request.Form.Files.Any())
            {
                var clarificationFileName = Request.Form.Files.First().FileName;
                var uploadedSuccessfully = await _fileStorageService.UploadFiles(applicationId, RoatpWorkflowSequenceIds.FinancialEvidence, 0, RoatpClarificationUpload.ClarificationFile, Request.Form.Files, ContainerType.Financial, new CancellationToken());

                if (!uploadedSuccessfully)
                {
                    _logger.LogError($"Unable to upload files for application: {applicationId} || File name {clarificationFileName}");
                    return BadRequest();
                }
                await _mediator.Send(new AddClarificationFileUploadRequest(applicationId, clarificationFileName));
            }

            return Ok();
        }

        [HttpGet("Clarification/Applications/{applicationId}/Download/{fileName}")]
        public async Task<IActionResult> DownloadClarificationFile(Guid applicationId, string fileName)
        {
            var file = await _fileStorageService.DownloadFile(applicationId, RoatpWorkflowSequenceIds.FinancialEvidence, 0, RoatpClarificationUpload.ClarificationFile, fileName, ContainerType.Financial, new CancellationToken());

            if (file is null)
            {
                _logger.LogError($"Unable to download file for application: {applicationId} || ClarificationId {RoatpClarificationUpload.ClarificationFile} || filename {fileName}");
                return NotFound();
            }

            return File(file.Stream, file.ContentType, file.FileName);
        }

        [HttpPost("Clarification/Applications/{applicationId}/Remove")]
        public async Task<IActionResult> RemoveClarificationFile(Guid applicationId,  [FromBody] RemoveClarificationFileCommand command)
        {
          
            var removedSuccessfully = await _fileStorageService.DeleteFile(applicationId, RoatpWorkflowSequenceIds.FinancialEvidence, 0, RoatpClarificationUpload.ClarificationFile, command.FileName, ContainerType.Financial, new CancellationToken());

            if (!removedSuccessfully)
            {
                _logger.LogError($"Unable to remove uploaded file for application: {applicationId} || File name {command.FileName}");
                return BadRequest();
            }
            await _mediator.Send(new RemoveClarificationFileUploadRequest(applicationId, command.FileName));
                
            return Ok();
        }

        [HttpPost("/Financial/{applicationId}/StartReview")]
        public async Task<ActionResult> StartReview(Guid applicationId, [FromBody] StartFinancialReviewApplicationRequest request)
        {
            await _mediator.Send(new StartFinancialReviewRequest(applicationId, request.Reviewer));
            return Ok();
        }


        private async Task<List<FinancialEvidence>> GetFinancialEvidence(Guid applicationId)
        {
            const int FinancialEvidenceSequence = 2;

            var listOfEvidence = new List<FinancialEvidence>();

            var financialSequence = await _qnaApiClient.GetSequenceBySequenceNo(applicationId, FinancialEvidenceSequence);
            var financialSections = await _qnaApiClient.GetSections(applicationId, financialSequence.Id);

            foreach (var financialSection in financialSections)
            {
                if (financialSection != null)
                {
                    var pagesContainingQuestionsWithFileupload = financialSection.QnAData.Pages.Where(x => x.Questions.Any(y => y.Input.Type == QuestionType.FileUpload)).ToList();
                    foreach (var uploadPage in pagesContainingQuestionsWithFileupload)
                    {
                        foreach (var uploadQuestion in uploadPage.Questions)
                        {
                            foreach (var answer in financialSection.QnAData.Pages.SelectMany(p => p.PageOfAnswers).SelectMany(a => a.Answers).Where(a => a.QuestionId == uploadQuestion.QuestionId))
                            {
                                if (string.IsNullOrWhiteSpace(answer.ToString())) continue;
                                var filenameWithFullPath = $"{financialSequence.ApplicationId}/{financialSequence.Id}/{financialSection.Id}/{uploadPage.PageId}/{uploadQuestion.QuestionId}/{answer.Value}";
                                listOfEvidence.Add(new FinancialEvidence { Filename = filenameWithFullPath });
                            }
                        }
                    }
                }
            }

            return listOfEvidence;
        }
    }

    public class StartFinancialReviewApplicationRequest
    {
        public string Reviewer { get; set; }
    }

    public class UploadClarificationFileCommand
    {
        public string UserId { get; set; }
    }

    public class RemoveClarificationFileCommand
    {
        public string FileName { get; set; }
        public string UserId { get; set; }
    }
}
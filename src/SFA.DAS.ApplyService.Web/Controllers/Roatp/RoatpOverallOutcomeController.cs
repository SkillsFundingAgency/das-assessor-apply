﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Roatp;
using SFA.DAS.ApplyService.Types;
using SFA.DAS.ApplyService.Web.Infrastructure;
using SFA.DAS.ApplyService.Web.Services;

namespace SFA.DAS.ApplyService.Web.Controllers.Roatp
{
    [Authorize]
    public class RoatpOverallOutcomeController : Controller
    {
        private readonly IOutcomeApiClient _apiClient;
        private readonly IApplicationApiClient _applicationApiClient;
        private readonly IOverallOutcomeService _overallOutcomeService;
        private readonly ILogger<RoatpOverallOutcomeController> _logger;
        private const string SupportingRouteId = "3";
        public RoatpOverallOutcomeController(IOutcomeApiClient apiClient, 
            IOverallOutcomeService overallOutcomeService, IApplicationApiClient applicationApiClient,
            ILogger<RoatpOverallOutcomeController> logger)
        {
            _apiClient = apiClient;
            _overallOutcomeService = overallOutcomeService;
            _logger = logger;
            _applicationApiClient = applicationApiClient;
        }



        [HttpGet]
        [Route("application/{applicationId}/sector/{pageId}")]
        public async Task<IActionResult> GetSectorDetails(Guid applicationId, string pageId)
        {
            var model = await _overallOutcomeService.GetSectorDetailsViewModel(applicationId, pageId);
            return View("~/Views/Roatp/ApplicationUnsuccessfulSectorAnswers.cshtml", model);
        }

        [HttpGet]
        [Authorize(Policy = "AccessApplication")]
        public async Task<IActionResult> ProcessApplicationStatus(Guid applicationId)
        {
            var application = await _applicationApiClient.GetApplication(applicationId);
            var model = _overallOutcomeService.BuildApplicationSummaryViewModel(application, User.GetEmail());

            switch (application.ApplicationStatus)
            {
                case ApplicationStatus.New:
                case ApplicationStatus.InProgress:
                    return RedirectToAction("TaskList", "RoatpApplication", new {applicationId});
                case ApplicationStatus.Successful:
               
                    if (model.ApplicationRouteId==SupportingRouteId)
                    {
                        return View("~/Views/Roatp/ApplicationApprovedSupporting.cshtml", model);
                    }

                    var oversightReview = await _apiClient.GetOversightReview(applicationId);
                    switch (oversightReview?.Status)
                    {
                        case OversightReviewStatus.SuccessfulAlreadyActive:
                            return View("~/Views/Roatp/ApplicationApprovedAlreadyActive.cshtml", model);
                        case OversightReviewStatus.SuccessfulFitnessForFunding:
                            return View("~/Views/Roatp/ApplicationApprovedFitnessForFunding.cshtml", model);
                        default:
                            return View("~/Views/Roatp/ApplicationApproved.cshtml", model);
                    }

                case ApplicationStatus.Unsuccessful: 
                    if (application.GatewayReviewStatus == GatewayReviewStatus.Fail)
                        return View("~/Views/Roatp/ApplicationUnsuccessful.cshtml", model);

                    var unsuccessfulModel =
                        await _overallOutcomeService.BuildApplicationSummaryViewModelWithGatewayAndModerationDetails(application,
                            User.GetEmail());
                    return View("~/Views/Roatp/ApplicationUnsuccessfulPostGateway.cshtml", unsuccessfulModel);

                case ApplicationStatus.FeedbackAdded:
                    return View("~/Views/Roatp/FeedbackAdded.cshtml", model);
                case ApplicationStatus.Withdrawn:
                    return View("~/Views/Roatp/ApplicationWithdrawn.cshtml", model);
                case ApplicationStatus.Removed:
                    var oversightReviewDetails = await _apiClient.GetOversightReview(applicationId);
                    if (oversightReviewDetails?.Status == OversightReviewStatus.Removed)
                        return View("~/Views/Roatp/ApplicationWithdrawnESFA.cshtml", model);
                    return View("~/Views/Roatp/ApplicationSubmitted.cshtml", model);
                case ApplicationStatus.GatewayAssessed:
                    if (application.GatewayReviewStatus == GatewayReviewStatus.Rejected)
                        return View("~/Views/Roatp/ApplicationRejected.cshtml", model);
                    return View("~/Views/Roatp/ApplicationSubmitted.cshtml", model);
                case ApplicationStatus.Rejected:
                    return View("~/Views/Roatp/ApplicationRejected.cshtml", model);   
                case ApplicationStatus.Submitted:
                case ApplicationStatus.Resubmitted:
                    return View("~/Views/Roatp/ApplicationSubmitted.cshtml", model);
                case ApplicationStatus.InProgressOutcome:
                    var oversightReviewNotes = await _apiClient.GetOversightReview(applicationId);
                    model.OversightInProgressExternalComments = oversightReviewNotes?.InProgressExternalComments;
                    return View("~/Views/Roatp/ApplicationInProgress.cshtml", model);
                default:
                    return RedirectToAction("TaskList", "RoatpApplication", new {applicationId});
            }
        }

        [HttpGet("ClarificationDownload/{applicationId}/Sequence/{sequenceNumber}/Section/{sectionNumber}/Page/{pageId}/Download/{fileName}")]
        public async Task<IActionResult> DownloadClarificationFile(Guid applicationId, int sequenceNumber, int sectionNumber, string pageId, string fileName)
        {
            var response = await _apiClient.DownloadClarificationfile(applicationId, sequenceNumber, sectionNumber, pageId, fileName);

            if (!response.IsSuccessStatusCode) return NotFound();
            var fileStream = await response.Content.ReadAsStreamAsync();
            return File(fileStream, response.Content.Headers.ContentType.MediaType, fileName);
        }
    }
}

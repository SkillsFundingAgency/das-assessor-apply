﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.ApplyService.Domain.Roatp;
using SFA.DAS.ApplyService.Session;
using SFA.DAS.ApplyService.Web.ViewModels.Roatp;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.Web.Controllers.Roatp
{
    [Authorize]
    public class RoatpShutterPagesController : Controller
    {
        private readonly ISessionService _sessionService;

        private const string ApplicationDetailsKey = "Roatp_Application_Details";

        public RoatpShutterPagesController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [Route("not-accept-terms-conditions")]
        public async Task<IActionResult> TermsAndConditionsNotAgreed()
        {
            return View("~/Views/Roatp/TermsAndConditionsNotAgreed.cshtml");
        }

        [Route("uk-provider-reference-number-not-found")]
        public async Task<IActionResult> UkprnNotFound()
        {
            var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);

            var viewModel = new UkprnSearchResultsViewModel
            {
                UKPRN = applicationDetails.UKPRN.ToString()
            };

            return View("~/Views/Roatp/UkprnNotFound.cshtml", viewModel);
        }

        [Route("company-not-found")]
        public async Task<IActionResult> CompanyNotFound()
        {
            var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);

            var viewModel = new UkprnSearchResultsViewModel
            {
                UKPRN = applicationDetails.UKPRN.ToString(),
                ProviderDetails = applicationDetails.UkrlpLookupDetails
            };

            return View("~/Views/Roatp/CompanyNotFound.cshtml", viewModel);
        }

        [Route("charity-not-found")]
        public async Task<IActionResult> CharityNotFound()
        {
            var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);

            var viewModel = new UkprnSearchResultsViewModel
            {
                UKPRN = applicationDetails.UKPRN.ToString(),
                ProviderDetails = applicationDetails.UkrlpLookupDetails
            };

            return View("~/Views/Roatp/CharityNotFound.cshtml", viewModel);
        }

        [Route("ukrlp-unavailable")]
        public async Task<IActionResult> UkrlpNotAvailable()
        {
            return View("~/Views/Roatp/UkrlpNotAvailable.cshtml");
        }

        [Route("companies-house-unavailable")]
        public async Task<IActionResult> CompaniesHouseNotAvailable()
        {
            return View("~/Views/Roatp/CompaniesHouseNotAvailable.cshtml");
        }

        [Route("charity-commission-unavailable")]
        public async Task<IActionResult> CharityCommissionNotAvailable()
        {
            return View("~/Views/Roatp/CharityCommissionNotAvailable.cshtml");
        }
        
        [Route("not-eligible")]
        public async Task<IActionResult> IneligibleToJoin()
        {
            return View("~/Views/Roatp/IneligibleToJoin.cshtml");
        }

    }
}
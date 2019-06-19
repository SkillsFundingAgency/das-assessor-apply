﻿namespace SFA.DAS.ApplyService.Web.Controllers.Roatp
{
    using System.Linq;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using SFA.DAS.ApplyService.Web.Infrastructure;
    using System.Threading.Tasks;
    using Domain.Roatp;
    using Session;
    using ViewModels.Roatp;
    using Validators;
    using Microsoft.AspNetCore.Authorization;

    [Authorize]
    public class RoatpApplicationPreambleController : Controller
    {
        private ILogger<RoatpApplicationPreambleController> _logger;
        private IRoatpApiClient _roatpApiClient;
        private IUkrlpApiClient _ukrlpApiClient;
        private ISessionService _sessionService;

        private const string ApplicationDetailsKey = "Roatp_Application_Details";

        public RoatpApplicationPreambleController(ILogger<RoatpApplicationPreambleController> logger, IRoatpApiClient roatpApiClient, 
                                                  IUkrlpApiClient ukrlpApiClient, ISessionService sessionService)
        {
            _logger = logger;
            _roatpApiClient = roatpApiClient;
            _ukrlpApiClient = ukrlpApiClient;
            _sessionService = sessionService;
        }

        [Route("select-application-route")]
        public async Task<IActionResult> SelectApplicationRoute()
        {
            var applicationRoutes = await _roatpApiClient.GetApplicationRoutes();

            var viewModel = new SelectApplicationRouteViewModel
            {
                ApplicationRoutes = applicationRoutes
            };

            return View("~/Views/Roatp/SelectApplicationRoute.cshtml", viewModel);
        }

        [Route("enter-your-ukprn")]
        public async Task<IActionResult> EnterApplicationUkprn(SelectApplicationRouteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ApplicationRoutes = await _roatpApiClient.GetApplicationRoutes();

                return View("~/Views/Roatp/SelectApplicationRoute.cshtml", model);
            }

            var applicationDetails = new ApplicationDetails
            {
                ApplicationRouteId = model.ApplicationRouteId
            };

            _sessionService.Set(ApplicationDetailsKey, applicationDetails);

            var viewModel = new SearchByUkprnViewModel { ApplicationRouteId = model.ApplicationRouteId };

            return View("~/Views/Roatp/EnterApplicationUkprn.cshtml", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SearchByUkprn(SearchByUkprnViewModel model)
        {
            long ukprn;
            if (!UkprnValidator.IsValidUkprn(model.UKPRN, out ukprn))
            {
                ModelState.AddModelError(nameof(model.UKPRN), "Enter a valid UKPRN");
                TempData["ShowErrors"] = true;

                return View("~/Views/Roatp/EnterApplicationUkprn.cshtml", model);
            }
            
            var matchingResults = await _ukrlpApiClient.GetTrainingProviderByUkprn(ukprn);

            if (matchingResults.Any())
            {
                var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);
                applicationDetails.UkrlpLookupDetails = matchingResults.FirstOrDefault();

                _sessionService.Set(ApplicationDetailsKey, applicationDetails);

                var registerStatus = await _roatpApiClient.UkprnOnRegister(ukprn);

                if (registerStatus.ExistingUKPRN)
                {
                    if (registerStatus.ProviderTypeId != applicationDetails.ApplicationRouteId
                        || registerStatus.StatusId == OrganisationRegisterStatus.RemovedStatus)
                    {
                        return RedirectToAction("UkprnFound");
                    }
                    else
                    {
                        return RedirectToAction("UkprnActive");
                    }
                }
                
                return RedirectToAction("UkprnFound");
            }
            else
            {
                var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);
                applicationDetails.UKPRN = ukprn;

                _sessionService.Set(ApplicationDetailsKey, applicationDetails);
                return RedirectToAction("UkprnNotFound");
            }
        }

        [Route("confirm-organisation-details")]
        public async Task<IActionResult> UkprnFound()
        {
            var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);

            var viewModel = new UkprnSearchResultsViewModel
            {
                ProviderDetails = applicationDetails.UkrlpLookupDetails,
                ApplicationRouteId = applicationDetails.ApplicationRouteId,
                UKPRN = applicationDetails.UkrlpLookupDetails.UKPRN
            };

            return View("~/Views/Roatp/UkprnFound.cshtml", viewModel);
        }

        [Route("organisation-not-found")]
        public async Task<IActionResult> UkprnNotFound()
        {
            var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);

            var viewModel = new UkprnSearchResultsViewModel
            {
                ApplicationRouteId = applicationDetails.ApplicationRouteId,
                UKPRN = applicationDetails.UKPRN.ToString()
            };

            return View("~/Views/Roatp/UkprnNotFound.cshtml", viewModel);
        }

        [Route("already-on-register")]
        public async Task<IActionResult> UkprnActive()
        {
            var applicationDetails = _sessionService.Get<ApplicationDetails>(ApplicationDetailsKey);

            var viewModel = new UkprnSearchResultsViewModel
            {
                ApplicationRouteId = applicationDetails.ApplicationRouteId,
                UKPRN = applicationDetails.UKPRN.ToString()
            };

            return View("~/Views/Roatp/UkprnActive.cshtml", viewModel);
        }
    }
}

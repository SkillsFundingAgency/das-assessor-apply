﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.InternalApi.Infrastructure;
using SFA.DAS.ApplyService.InternalApi.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.InternalApi.Controllers
{
    public class OrganisationSearchController : Controller
    {
        private readonly ILogger<OrganisationSearchController> _logger;
        private readonly AssessorServiceApiClient _assessorServiceApiClient;
        private readonly ProviderRegisterApiClient _providerRegisterApiClient;
        private readonly ReferenceDataApiClient _referenceDataApiClient;

        public OrganisationSearchController(ILogger<OrganisationSearchController> logger, AssessorServiceApiClient assessorServiceApiClient, ProviderRegisterApiClient providerRegisterApiClient, ReferenceDataApiClient referenceDataApiClient)
        {
            _logger = logger;
            _assessorServiceApiClient = assessorServiceApiClient;
            _providerRegisterApiClient = providerRegisterApiClient;
            _referenceDataApiClient = referenceDataApiClient;
        }

        [HttpGet("OrganisationSearch")]
        public async Task<IEnumerable<Organisation>> OrganisationSearch(string searchTerm)
        {
            IEnumerable<Organisation> results = null;

            // FIRST - Search EPAO Register
            if (results == null || !results.Any())
            {
                try
                {
                    _logger.LogInformation($"Searching EPAO Register for. Search Term: {searchTerm}");
                    results = await _assessorServiceApiClient.SearchOrgansiation(searchTerm);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error from EPAO Register. Message: {ex.Message}");
                }
            }

            // SECOND - Search Provider Register 
            if (results == null || !results.Any())
            {
                try
                {
                    _logger.LogInformation($"Searching Provider Register. Search Term: {searchTerm}");
                    results = await _providerRegisterApiClient.SearchOrgansiation(searchTerm);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Error from Provider Register. Message: {ex.Message}");
                }
            }

            // THIRD - Use Reference Data API
            if (results == null || !results.Any())
            {
                try
                {
                    _logger.LogInformation($"Searching Reference Data API. Search Term: {searchTerm}");
                    results = await _referenceDataApiClient.SearchOrgansiation(searchTerm);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Error from Reference Data API. Message: {ex.Message}");
                }
            }

            return results;
        }

        [HttpGet("OrganisationByEmail")]
        public async Task<Organisation> GetOrganisationByEmail(string emailAddress)
        {
            Organisation result = null;

            try
            {
                _logger.LogInformation($"Searching EPAO Register for. Email: {emailAddress}");
                result = await _assessorServiceApiClient.GetOrganisationByEmail(emailAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error from EPAO Register. Message: {ex.Message}");
            }

            return result;
        }

        [HttpGet("OrganisationTypes")]
        public async Task<IEnumerable<string>> GetOrganisationTypes()
        {
            IEnumerable<string> results = null;

            try
            {
                _logger.LogInformation($"Getting Organisation Types from EPAO Register.");
                results = await _assessorServiceApiClient.GetOrgansiationTypes();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error from EPAO Register. Message: {ex.Message}");
            }

            return results;
        }
    }
}
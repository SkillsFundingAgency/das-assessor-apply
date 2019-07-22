﻿using AutoMapper;
using CharityCommissionService;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Configuration;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.InternalApi.Infrastructure
{
    /// <summary>
    /// Charity Commission API docs are located at: http://apps.charitycommission.gov.uk/Showcharity/API/SearchCharitiesV1/Docs/DevGuideHome.aspx
    /// Charity Commission WSDL is located at: https://apps.charitycommission.gov.uk/Showcharity/API/SearchCharitiesV1/SearchCharitiesV1.asmx?WSDL
    /// There is a Web-Friendly version located at: http://beta.charitycommission.gov.uk/charity-search/
    /// </summary>
    public class CharityCommissionApiClient
    {
        private readonly ISearchCharitiesV1SoapClient _client;
        private readonly ILogger<CharityCommissionApiClient> _logger;
        private readonly IApplyConfig _config;

        public CharityCommissionApiClient(ISearchCharitiesV1SoapClient client, ILogger<CharityCommissionApiClient> logger, IConfigurationService configurationService)
        {
            _client = client;
            _logger = logger;
            _config = configurationService.GetConfig().GetAwaiter().GetResult();
        }

        public async Task<Types.CharityCommission.Charity> GetCharity(int charityNumber)
        {
            var charity = await GetCharityDetails(charityNumber);

            if (charity != null)
            {
                // nothing to do at the moment
            }

            return charity;
        }

        public async Task<bool> IsCharityActivelyTrading(int charityNumber)
        {
            var isTrading = false;

            var charity = await GetCharityDetails(charityNumber);

            if (charity != null)
            {
                isTrading = "registered".Equals(charity.Status, StringComparison.InvariantCultureIgnoreCase) && charity.DissolvedOn == null;
            }

            return isTrading;
        }

        private async Task<Types.CharityCommission.Charity> GetCharityDetails(int charityNumber)
        {
            _logger.LogInformation($"Searching Charity Commission - Charity Details. Charity Number: {charityNumber}");
            var request = new GetCharityByRegisteredCharityNumberRequest(_config.CharityCommissionApiAuthentication.ApiKey, charityNumber);

            try
            {
                var apiResponse = await _client.GetCharityByRegisteredCharityNumberAsync(request);
                return Mapper.Map<Charity, Types.CharityCommission.Charity>(apiResponse.GetCharityByRegisteredCharityNumberResult);
            }
            catch (Exception soapEx)
            {
                _logger.LogError(soapEx, $"GET: HTTP Error when processing request to GetCharityDetails: {charityNumber}");
                throw;
            }
        }
    }
}

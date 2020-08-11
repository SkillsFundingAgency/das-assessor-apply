﻿using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SFA.DAS.ApplyService.Session;
using SFA.DAS.ApplyService.Web.Configuration;
using SFA.DAS.ApplyService.Web.Infrastructure;
using System;
using System.Collections.Generic;

namespace SFA.DAS.ApplyService.Web.Services
{
    public class NotRequiredOverridesService : INotRequiredOverridesService
    {
        private List<NotRequiredOverrideConfiguration> _configuration;
        private readonly IQnaApiClient _qnaApiClient;
        private readonly ISessionService _sessionService;
        private const string NotRequiredConfigSessionKeyFormat = "NotRequiredConfiguration_{0}";

        // TODO: for story APR-1152, implement link to repo interface that:
        // 1. tries to fetch the config from the API repository
        // 2. if present, use as source of truth
        // 3. if not present, retrieve from appsettings.json, and store to repository via API
        
        public NotRequiredOverridesService(IOptions<List<NotRequiredOverrideConfiguration>> notRequiredOverrides, 
                                           IQnaApiClient qnaApiClient,
                                           ISessionService sessionService)
        {
            _configuration = notRequiredOverrides.Value;
            _qnaApiClient = qnaApiClient;
            _sessionService = sessionService;
        }

        public List<NotRequiredOverrideConfiguration> GetNotRequiredOverrides(Guid applicationId)
        {
            var sessionKey = string.Format(NotRequiredConfigSessionKeyFormat, applicationId);
            var configuration = _sessionService.Get<List<NotRequiredOverrideConfiguration>>(sessionKey);
            if (configuration != null)
            {
                return configuration;
            }

            PopulateNotRequiredOverridesWithApplicationData(applicationId);
            return _configuration;
        }

        private void PopulateNotRequiredOverridesWithApplicationData(Guid applicationId)
        {
            var applicationData =  _qnaApiClient.GetApplicationData(applicationId).GetAwaiter().GetResult() as JObject;

            if (applicationData == null)
            {
                return;
            }

            foreach (var overrideConfig in _configuration)
            {
                foreach (var condition in overrideConfig.Conditions)
                {
                    var applicationDataValue = applicationData[condition.ConditionalCheckField];
                    condition.Value = applicationDataValue != null ? applicationDataValue.Value<string>() : string.Empty;
                }
            }

            var sessionKey = string.Format(NotRequiredConfigSessionKeyFormat, applicationId);
            _sessionService.Set(sessionKey, _configuration);
        }
    }
}
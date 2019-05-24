﻿using AutoMapper;
using SFA.DAS.ApplyService.Web.AutoMapper;

namespace SFA.DAS.ApplyService.Web
{
    public static class MappingStartup
    {
        public static void AddMappings()
        {
            Mapper.Reset();

            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<CompaniesHouseSummaryProfile>();
                cfg.AddProfile<DirectorInformationProfile>();
                cfg.AddProfile<PersonSignificantControlInformationProfile>();
            });

            Mapper.AssertConfigurationIsValid();
        }
    }
}

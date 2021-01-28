﻿using System;
using System.Collections.Generic;

namespace SFA.DAS.ApplyService.InternalApi.Types.QueryResults
{
    public class PendingOversightReviews
    {
        public List<PendingOversightReview> Reviews { get; set; }
    }

    public class PendingOversightReview
    {
        public Guid ApplicationId { get; set; }
        public string OrganisationName { get; set; }

        public string Ukprn { get; set; }
        public string ProviderRoute { get; set; }
        public string ApplicationReferenceNumber { get; set; }
        public DateTime ApplicationSubmittedDate { get; set; }
    }
}
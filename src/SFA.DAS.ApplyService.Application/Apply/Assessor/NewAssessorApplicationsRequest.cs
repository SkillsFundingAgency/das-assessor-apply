﻿using System.Collections.Generic;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class NewAssessorApplicationsRequest : IRequest<List<AssessorApplicationSummary>>
    {
        public NewAssessorApplicationsRequest(string userId, string sortOrder)
        {
            UserId = userId;
            SortOrder = sortOrder;
        }

        public string UserId { get; }
        public string SortOrder { get; }
    }
}

using System.Collections.Generic;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class ClosedAssessorApplicationsRequest : IRequest<List<ClosedApplicationSummary>>
    {
        public ClosedAssessorApplicationsRequest(string userId, string searchTerm, string sortColumn, string sortOrder)
        {
            UserId = userId;
            SearchTerm = searchTerm;
            SortColumn = sortColumn;
            SortOrder = sortOrder;
            
        }

        public string UserId { get; }
        public string SearchTerm { get; }
        public string SortColumn { get; }
        public string SortOrder { get; }
    }
}

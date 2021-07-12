using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;

namespace SFA.DAS.ApplyService.Application.Apply.Financial.Applications
{
    public class OpenFinancialApplicationsRequest : IRequest<List<RoatpFinancialSummaryItem>>
    {
        public OpenFinancialApplicationsRequest(string searchTerm, string sortColumn, string sortOrder)
        {
            SearchTerm = searchTerm;
            SortOrder = sortOrder;
            SortColumn = sortColumn;
        }

        public string SearchTerm { get; }
        public string SortColumn { get; }
        public string SortOrder { get; }
    }
}

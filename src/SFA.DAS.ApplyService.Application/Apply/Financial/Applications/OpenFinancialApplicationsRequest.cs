using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;

namespace SFA.DAS.ApplyService.Application.Apply.Financial.Applications
{
    public class OpenFinancialApplicationsRequest : IRequest<List<RoatpFinancialSummaryItem>>
    {
        public OpenFinancialApplicationsRequest(string sortOrder, string sortColumn)
        {
            SortOrder = sortOrder;
            SortColumn = sortColumn;
        }

        public string SortColumn { get; set; }
        public string SortOrder { get; }
    }
}

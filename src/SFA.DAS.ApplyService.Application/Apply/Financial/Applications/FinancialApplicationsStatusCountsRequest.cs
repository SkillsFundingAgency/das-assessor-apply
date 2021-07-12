using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;

namespace SFA.DAS.ApplyService.Application.Apply.Financial.Applications
{
    public class FinancialApplicationsStatusCountsRequest : IRequest<RoatpFinancialApplicationsStatusCounts>
    {
        public FinancialApplicationsStatusCountsRequest(string searchTerm)
        {
            SearchTerm = searchTerm;
        }

        public string SearchTerm { get; }
    }
}

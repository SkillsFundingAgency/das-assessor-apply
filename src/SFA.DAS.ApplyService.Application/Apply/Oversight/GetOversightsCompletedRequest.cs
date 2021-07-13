using MediatR;
using SFA.DAS.ApplyService.Domain.QueryResults;

namespace SFA.DAS.ApplyService.Application.Apply.Oversight
{
    public class GetOversightsCompletedRequest : IRequest<CompletedOversightReviews>
    {
        public GetOversightsCompletedRequest(string searchTerm, string sortColumn, string sortOrder)
        {
            SearchTerm = searchTerm;
            SortColumn = sortColumn;
            SortOrder = sortOrder;
        }

        public string SearchTerm { get; }
        public string SortColumn { get; }
        public string SortOrder { get; }
    }
}


using MediatR;
using SFA.DAS.ApplyService.Domain.QueryResults;

namespace SFA.DAS.ApplyService.Application.Apply.Oversight
{
    public class GetOversightsCompletedRequest : IRequest<CompletedOversightReviews>
    {
        public GetOversightsCompletedRequest(string sortColumn, string sortOrder)
        {
            SortOrder = sortOrder;
            SortColumn = sortColumn;
        }

        public string SortColumn { get; set; }
        public string SortOrder { get; }
    }
}


using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Application.Interfaces;
using SFA.DAS.ApplyService.Domain.Interfaces;
using SFA.DAS.ApplyService.Domain.QueryResults;

namespace SFA.DAS.ApplyService.Application.Apply.Oversight
{
    public class GetOversightsPendingHandler : IRequestHandler<GetOversightsPendingRequest, PendingOversightReviews>
    {
        private readonly IOversightReviewQueries _oversightReviewQueries;

        public GetOversightsPendingHandler(IOversightReviewQueries oversightReviewQueries)
        {
            _oversightReviewQueries = oversightReviewQueries;
        }

        public async Task<PendingOversightReviews> Handle(GetOversightsPendingRequest request, CancellationToken cancellationToken)
        {
            return await _oversightReviewQueries.GetPendingOversightReviews(request.SortColumn,request.SortOrder);
        }
    }
}

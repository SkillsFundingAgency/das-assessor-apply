using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Gateway.Applications
{
    public class GetGatewayApplicationsCountsHandler : IRequestHandler<GetGatewayApplicationCountsRequest, GetGatewayApplicationCountsResponse>
    {
        private readonly IGatewayRepository _repository;

        public GetGatewayApplicationsCountsHandler(IGatewayRepository repository)
        {
            _repository = repository;
        }

        public async Task<GetGatewayApplicationCountsResponse> Handle(GetGatewayApplicationCountsRequest request, CancellationToken cancellationToken)
        {
            var newGatewayStatuses = new List<string> { GatewayReviewStatus.New };
            var inProgressGatewayStatuses = new List<string> { GatewayReviewStatus.InProgress, GatewayReviewStatus.ClarificationSent };
            var closedGatewayStatuses = new List<string> { GatewayReviewStatus.Fail, GatewayReviewStatus.Pass, GatewayReviewStatus.Rejected };

            var counts = (await _repository.GetGatewayApplicationStatusCounts(request.SearchTerm)).ToList();

            var response = new GetGatewayApplicationCountsResponse
            {
                NewApplicationsCount = counts.Where(x => x.ApplicationStatus == ApplicationStatus.Submitted &&
                                                    newGatewayStatuses.Contains(x.GatewayReviewStatus))
                                        .Sum(x => x.Count),

                InProgressApplicationsCount = counts.Where(x => x.ApplicationStatus == ApplicationStatus.Submitted &&
                                                    inProgressGatewayStatuses.Contains(x.GatewayReviewStatus))
                                        .Sum(x => x.Count),

                ClosedApplicationsCount = counts.Where(x => x.ApplicationStatus == ApplicationStatus.Withdrawn ||
                                                    x.ApplicationStatus == ApplicationStatus.Removed ||
                                                    closedGatewayStatuses.Contains(x.GatewayReviewStatus))
                                        .Sum(x => x.Count)
            };

            return response;
        }
    }
}
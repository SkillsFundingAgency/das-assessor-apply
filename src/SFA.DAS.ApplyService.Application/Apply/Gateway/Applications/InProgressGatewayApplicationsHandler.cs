using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Gateway.Applications
{
    public class InProgressGatewayApplicationsHandler : IRequestHandler<InProgressGatewayApplicationsRequest, List<RoatpGatewaySummaryItem>>
    {
        private readonly IGatewayRepository _repository;

        public InProgressGatewayApplicationsHandler(IGatewayRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoatpGatewaySummaryItem>> Handle(InProgressGatewayApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetInProgressGatewayApplications(request.SearchTerm, request.SortColumn, request.SortOrder);
        }
    }
}

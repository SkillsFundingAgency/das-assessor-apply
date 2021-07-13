using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Gateway.Applications
{
    public class NewGatewayApplicationsHandler : IRequestHandler<NewGatewayApplicationsRequest, List<RoatpGatewaySummaryItem>>
    {
        private readonly IGatewayRepository _repository;

        public NewGatewayApplicationsHandler(IGatewayRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoatpGatewaySummaryItem>> Handle(NewGatewayApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetNewGatewayApplications(request.SearchTerm, request.SortOrder);
        }
    }
}

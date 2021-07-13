using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;

namespace SFA.DAS.ApplyService.Application.Apply.Gateway.Applications
{
    public class ClosedGatewayApplicationsRequest : IRequest<List<RoatpGatewaySummaryItem>>
    {
        public string SearchTerm { get; set; }
        public string SortColumn { get; set; }
        public string SortOrder { get; set; }
    }
}

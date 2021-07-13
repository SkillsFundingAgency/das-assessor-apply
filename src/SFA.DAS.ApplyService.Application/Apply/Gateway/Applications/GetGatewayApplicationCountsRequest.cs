using MediatR;

namespace SFA.DAS.ApplyService.Application.Apply.Gateway.Applications
{
    public class GetGatewayApplicationCountsRequest : IRequest<GetGatewayApplicationCountsResponse>
    {
        public string SearchTerm { get; set; }
    }
}

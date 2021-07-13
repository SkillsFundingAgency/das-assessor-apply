using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Financial.Applications
{
    public class ClosedFinancialApplicationsHandler : IRequestHandler<ClosedFinancialApplicationsRequest, List<RoatpFinancialSummaryItem>>
    {
        private readonly IApplyRepository _repository;

        public ClosedFinancialApplicationsHandler(IApplyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoatpFinancialSummaryItem>> Handle(ClosedFinancialApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetClosedFinancialApplications(request.SortOrder, request.SortColumn);
        }
    }
}

using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Financial.Applications
{
    public class ClarificationFinancialApplicationsHandler : IRequestHandler<ClarificationFinancialApplicationsRequest, List<RoatpFinancialSummaryItem>>
    {
        private readonly IApplyRepository _repository;

        public ClarificationFinancialApplicationsHandler(IApplyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoatpFinancialSummaryItem>> Handle(ClarificationFinancialApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetClarificationFinancialApplications(request.SortOrder,request.SortColumn);
        }
    }
}

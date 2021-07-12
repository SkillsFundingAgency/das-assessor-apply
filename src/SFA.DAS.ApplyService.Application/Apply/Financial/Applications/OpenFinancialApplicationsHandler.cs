using MediatR;
using SFA.DAS.ApplyService.Domain.Apply;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Financial.Applications
{
    public class OpenFinancialApplicationsHandler : IRequestHandler<OpenFinancialApplicationsRequest, List<RoatpFinancialSummaryItem>>
    {
        private readonly IApplyRepository _repository;

        public OpenFinancialApplicationsHandler(IApplyRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<RoatpFinancialSummaryItem>> Handle(OpenFinancialApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetOpenFinancialApplications(request.SearchTerm, request.SortColumn, request.SortOrder);
        }
    }
}

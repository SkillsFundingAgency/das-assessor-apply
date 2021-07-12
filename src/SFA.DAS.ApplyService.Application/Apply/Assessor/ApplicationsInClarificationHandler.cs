using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class ApplicationsInClarificationHandler : IRequestHandler<ApplicationsInClarificationRequest, List<ClarificationApplicationSummary>>
    {
        private readonly IAssessorRepository _repository;

        public ApplicationsInClarificationHandler(IAssessorRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ClarificationApplicationSummary>> Handle(ApplicationsInClarificationRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetApplicationsInClarification(request.SearchTerm, request.SortColumn, request.SortOrder);
        }
    }
}

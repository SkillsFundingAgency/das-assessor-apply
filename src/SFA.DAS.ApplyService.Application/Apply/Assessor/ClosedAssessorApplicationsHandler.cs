using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class ClosedAssessorApplicationsHandler : IRequestHandler<ClosedAssessorApplicationsRequest, List<ClosedApplicationSummary>>
    {
        private readonly IAssessorRepository _repository;

        public ClosedAssessorApplicationsHandler(IAssessorRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ClosedApplicationSummary>> Handle(ClosedAssessorApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetClosedApplications(request.SortOrder, request.SortColumn);
        }
    }
}

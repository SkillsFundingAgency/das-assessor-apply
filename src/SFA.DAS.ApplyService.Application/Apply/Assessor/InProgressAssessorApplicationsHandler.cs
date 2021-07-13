using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class InProgressAssessorApplicationsHandler : IRequestHandler<InProgressAssessorApplicationsRequest, List<AssessorApplicationSummary>>
    {
        private readonly IAssessorRepository _repository;

        public InProgressAssessorApplicationsHandler(IAssessorRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<AssessorApplicationSummary>> Handle(InProgressAssessorApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetInProgressAssessorApplications(request.UserId, request.SortOrder, request.SortColumn);
        }
    }
}

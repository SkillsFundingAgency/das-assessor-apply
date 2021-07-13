using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class NewAssessorApplicationsHandler : IRequestHandler<NewAssessorApplicationsRequest, List<AssessorApplicationSummary>>
    {
        private readonly IAssessorRepository _repository;

        public NewAssessorApplicationsHandler(IAssessorRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<AssessorApplicationSummary>> Handle(NewAssessorApplicationsRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetNewAssessorApplications(request.UserId, request.SortOrder,request.SortColumn);
        }
    }
}

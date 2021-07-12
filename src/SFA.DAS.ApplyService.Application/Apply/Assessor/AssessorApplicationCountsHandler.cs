using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class AssessorApplicationCountsHandler : IRequestHandler<AssessorApplicationCountsRequest, AssessorApplicationCounts>
    {
        private readonly IAssessorRepository _repository;

        public AssessorApplicationCountsHandler(IAssessorRepository repository)
        {
            _repository = repository;
        }

        public async Task<AssessorApplicationCounts> Handle(AssessorApplicationCountsRequest request, CancellationToken cancellationToken)
        {
            var newApplicationCount = _repository.GetNewAssessorApplicationsCount(request.UserId, request.SearchTerm);
            var inProgressApplicationCount = _repository.GetInProgressAssessorApplicationsCount(request.UserId, request.SearchTerm);
            var inModerationApplicationCount = _repository.GetApplicationsInModerationCount(request.SearchTerm);
            var inClarificationApplicationCount = _repository.GetApplicationsInClarificationCount(request.SearchTerm);
            var closedApplicationCount = _repository.GetClosedApplicationsCount(request.SearchTerm);

            await Task.WhenAll(newApplicationCount, inProgressApplicationCount, inModerationApplicationCount, inClarificationApplicationCount, closedApplicationCount);

            return new AssessorApplicationCounts(newApplicationCount.Result, inProgressApplicationCount.Result, inModerationApplicationCount.Result, inClarificationApplicationCount.Result, closedApplicationCount.Result);
        }
    }
}

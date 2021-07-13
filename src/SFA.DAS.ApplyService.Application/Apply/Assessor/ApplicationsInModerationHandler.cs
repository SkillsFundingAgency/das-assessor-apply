using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class ApplicationsInModerationHandler : IRequestHandler<ApplicationsInModerationRequest, List<ModerationApplicationSummary>>
    {
        private readonly IAssessorRepository _repository;

        public ApplicationsInModerationHandler(IAssessorRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<ModerationApplicationSummary>> Handle(ApplicationsInModerationRequest request, CancellationToken cancellationToken)
        {
            return await _repository.GetApplicationsInModeration(request.SortOrder, request.SortColumn);
        }
    }
}

using MediatR;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;

namespace SFA.DAS.ApplyService.Application.Apply.Assessor
{
    public class AssessorApplicationCountsRequest : IRequest<AssessorApplicationCounts>
    {
        public AssessorApplicationCountsRequest(string userId, string searchTerm)
        {
            UserId = userId;
            SearchTerm = searchTerm;
        }

        public string UserId { get; }
        public string SearchTerm { get; }
    }
}

using System;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.QueryResults;

namespace SFA.DAS.ApplyService.Domain.Interfaces
{
    public interface IOversightReviewQueries
    {
        Task<PendingOversightReviews> GetPendingOversightReviews(string searchTerm, string sortColumn, string sortOrder);
        Task<CompletedOversightReviews> GetCompletedOversightReviews(string searchTerm, string sortColumn,string sortOrder);
        Task<ApplicationOversightDetails> GetOversightApplicationDetails(Guid applicationId);
        Task<OversightReview> GetOversightReview(Guid applicationId);
    }
}

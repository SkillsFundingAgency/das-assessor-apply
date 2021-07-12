using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;

namespace SFA.DAS.ApplyService.Domain.Interfaces
{
    public interface IAssessorRepository
    {
        Task<List<AssessorApplicationSummary>> GetNewAssessorApplications(string userId, string searchTerm, string sortColumn, string sortOrder);
        Task<int> GetNewAssessorApplicationsCount(string userId, string searchTerm);
        Task<List<AssessorApplicationSummary>> GetInProgressAssessorApplications(string userId, string searchTerm, string sortColumn, string sortOrder);
        Task<int> GetInProgressAssessorApplicationsCount(string userId, string searchTerm);
        Task<List<ModerationApplicationSummary>> GetApplicationsInModeration(string searchTerm, string sortColumn, string sortOrder);
        Task<int> GetApplicationsInModerationCount(string searchTerm);
        Task<List<ClarificationApplicationSummary>> GetApplicationsInClarification(string searchTerm, string sortColumn, string sortOrder);
        Task<int> GetApplicationsInClarificationCount(string searchTerm);
        Task<List<ClosedApplicationSummary>> GetClosedApplications(string searchTerm, string sortColumn, string sortOrder);
        Task<int> GetClosedApplicationsCount(string searchTerm);
        Task AssignAssessor1(Guid applicationId, string userId, string userName);
        Task AssignAssessor2(Guid applicationId, string userId, string userName);
        Task SubmitAssessorPageOutcome(Guid applicationId, int sequenceNumber, int sectionNumber, string pageId, string userId, string userName, string status, string comment);
        Task<AssessorPageReviewOutcome> GetAssessorPageReviewOutcome(Guid applicationId, int sequenceNumber, int sectionNumber, string pageId, string userId);
        Task<List<AssessorPageReviewOutcome>> GetAssessorPageReviewOutcomesForSection(Guid applicationId, int sequenceNumber, int sectionNumber, string userId);
        Task<List<AssessorPageReviewOutcome>> GetAllAssessorPageReviewOutcomes(Guid applicationId, string userId);
        Task UpdateAssessorReviewStatus(Guid applicationId, string userId, string status);
        Task CreateEmptyAssessorReview(Guid applicationId, string userId, string userName, List<AssessorPageReviewOutcome> pageReviewOutcomes);
    }
}
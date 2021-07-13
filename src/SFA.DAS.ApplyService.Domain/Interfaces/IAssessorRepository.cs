using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;

namespace SFA.DAS.ApplyService.Domain.Interfaces
{
    public interface IAssessorRepository
    {
        Task<List<AssessorApplicationSummary>> GetNewAssessorApplications(string userId, string sortOrder, string sortColumn);
        Task<int> GetNewAssessorApplicationsCount(string userId);
        Task<List<AssessorApplicationSummary>> GetInProgressAssessorApplications(string userId, string sortOrder, string sortColumn);
        Task<int> GetInProgressAssessorApplicationsCount(string userId);
        Task<List<ModerationApplicationSummary>> GetApplicationsInModeration(string sortOrder, string sortColumn);
        Task<int> GetApplicationsInModerationCount();
        Task<List<ClarificationApplicationSummary>> GetApplicationsInClarification(string sortOrder, string sortColumn);
        Task<int> GetApplicationsInClarificationCount();
        Task<List<ClosedApplicationSummary>> GetClosedApplications(string sortOrder, string sortColumn);
        Task<int> GetClosedApplicationsCount();
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
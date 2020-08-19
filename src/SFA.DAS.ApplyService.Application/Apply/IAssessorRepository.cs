using SFA.DAS.ApplyService.Domain.Apply;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.InternalApi.Types;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;

namespace SFA.DAS.ApplyService.Application.Apply
{
    public interface IAssessorRepository
    {
        Task<List<RoatpAssessorApplicationSummary>> GetNewAssessorApplications(string userId);
        Task<int> GetNewAssessorApplicationsCount(string userId);
        Task UpdateAssessor1(Guid applicationId, string userId, string userName);
        Task UpdateAssessor2(Guid applicationId, string userId, string userName);
        Task<List<RoatpAssessorApplicationSummary>> GetInProgressAssessorApplications(string userId);
        Task<AssessorType> GetAssessorType(Guid applicationId, string userId);
        Task<int> GetInProgressAssessorApplicationsCount(string userId);
        Task<List<RoatpModerationApplicationSummary>> GetApplicationsInModeration();
        Task<int> GetApplicationsInModerationCount();
        Task SubmitAssessorPageOutcome(Guid applicationId, int sequenceNumber, int sectionNumber, string pageId, int assessorType, string userId, string status, string comment);
        Task<AssessorPageReviewOutcome> GetAssessorPageReviewOutcome(Guid applicationId, int sequenceNumber, int sectionNumber, string pageId, int assessorType, string userId);
        Task<List<AssessorPageReviewOutcome>> GetAssessorPageReviewOutcomesForSection(Guid applicationId, int sequenceNumber, int sectionNumber, int assessorType, string userId);
        Task<List<AssessorPageReviewOutcome>> GetAllAssessorPageReviewOutcomes(Guid applicationId, int assessorType, string userId);
        Task UpdateAssessorReviewStatus(Guid applicationId, int assessorType, string userId, string status);
    }
}
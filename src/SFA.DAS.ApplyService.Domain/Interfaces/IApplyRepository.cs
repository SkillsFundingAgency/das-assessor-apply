using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Roatp;

namespace SFA.DAS.ApplyService.Domain.Interfaces
{
    public interface IApplyRepository
    {
        Task<Guid> StartApplication(Guid applicationId, ApplyData applyData, Guid organisationId, Guid createdBy);

        Task<Domain.Entities.Apply> GetApplication(Guid applicationId);
        Task<Domain.Entities.Apply> GetApplicationByUserId(Guid applicationId, Guid signinId);
        Task<List<Domain.Entities.Apply>> GetUserApplications(Guid signinId);
        Task<List<Domain.Entities.Apply>> GetOrganisationApplications(Guid signinId);
               
        Task UpdateApplication(Domain.Entities.Apply application);
        
        Task<bool> CanSubmitApplication(Guid applicationId);
        Task<bool> SubmitApplication(Guid applicationId, ApplyData applyData, FinancialData financialData, Guid submittedBy);

        Task<Guid> SnapshotApplication(Guid applicationId, Guid snapshotApplicationId, List<ApplySequence> newSequences);

        Task<List<RoatpFinancialSummaryItem>> GetOpenFinancialApplications(string sortOrder, string sortColumn);
        Task<List<RoatpFinancialSummaryDownloadItem>> GetOpenFinancialApplicationsForDownload();
        Task<List<RoatpFinancialSummaryItem>> GetClarificationFinancialApplications(string sortOrder, string sortColumn);
        Task<List<RoatpFinancialSummaryItem>> GetClosedFinancialApplications(string sortOrder, string sortColumn);
        Task<RoatpFinancialApplicationsStatusCounts> GetFinancialApplicationsStatusCounts();
        Task<bool> StartFinancialReview(Guid applicationId, string reviewer);
        Task<bool> RecordFinancialGrade(Guid applicationId, FinancialReviewDetails financialReviewDetails, string financialReviewStatus);

        Task<bool> UpdateFinancialReviewDetails(Guid applicationId, FinancialReviewDetails financialReviewDetails);

        Task<IEnumerable<RoatpApplicationStatus>> GetExistingApplicationStatusByUkprn(string ukprn);

        Task<string> GetNextRoatpApplicationReference();

        Task<bool> StartAssessorReview(Guid applicationId, string reviewer);  
        
        Task<ApplyData> GetApplyData(Guid applicationId);

        Task<bool> UpdateApplyData(Guid applicationId, ApplyData applyData, string updatedBy);

        Task<bool> ChangeProviderRoute(Guid applicationId, int providerRoute, string providerRouteName);

        Task<bool> IsUkprnWhitelisted(int ukprn);

        Task UpdateApplicationStatus(Guid applicationId, string status, string userId);

        Task<Contact> GetContactForApplication(Guid applicationId);
        Task<Organisation> GetOrganisationForApplication(Guid applicationId);
        Task<List<ApplicationOversightDownloadDetails>> GetOversightsForDownload(DateTime dateFrom, DateTime dateTo);
        
        
    }
}
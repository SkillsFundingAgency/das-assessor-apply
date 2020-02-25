using Dapper;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Application.Apply;
using SFA.DAS.ApplyService.Application.Apply.Submit;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.Data.DapperTypeHandlers;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Roatp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.ApplyService.Data
{
    public class ApplyRepository : IApplyRepository
    {
        private readonly IApplyConfig _config;
        private readonly ILogger<ApplyRepository> _logger;

        public ApplyRepository(IConfigurationService configurationService, ILogger<ApplyRepository> logger)
        {
            _logger = logger;
            _config = configurationService.GetConfig().Result;

            SqlMapper.AddTypeHandler(typeof(ApplyData), new ApplyDataHandler());

            SqlMapper.AddTypeHandler(typeof(OrganisationDetails), new OrganisationDetailsHandler());
            SqlMapper.AddTypeHandler(typeof(QnAData), new QnADataHandler());
            SqlMapper.AddTypeHandler(typeof(ApplicationData), new ApplicationDataHandler());
            SqlMapper.AddTypeHandler(typeof(FinancialApplicationGrade), new FinancialApplicationGradeDataHandler());
        }



        public async Task<Guid> StartApplication(Guid applicationId, ApplyData applyData, Guid organisationId, Guid createdBy)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleAsync<Guid>(
                    @"INSERT INTO Apply (ApplicationId, OrganisationId, ApplicationStatus, ApplyData, CreatedBy, CreatedAt)
                                        OUTPUT INSERTED.[ApplicationId] 
                                        VALUES (@applicationId, @organisationId, @applicationStatus, @applyData, @createdBy, GETUTCDATE())",
                    new { applicationId, organisationId, applicationStatus = ApplicationStatus.InProgress, applyData, createdBy });
            }
        }

        public async Task<Domain.Entities.Apply> GetApplication(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var application = await connection.QuerySingleOrDefaultAsync<Domain.Entities.Apply>(@"SELECT * FROM apply WHERE ApplicationId = @applicationId", new { applicationId });

                //if (application != null)
                //{
                //    application.ApplyingOrganisation = await GetOrganisationForApplication(applicationId);
                //}

                return application;
            }
        }

        public async Task<List<Domain.Entities.Apply>> GetUserApplications(Guid userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection.QueryAsync<Domain.Entities.Apply>(@"SELECT a.* FROM Contacts c
                                                    INNER JOIN Apply a ON a.OrganisationId = c.ApplyOrganisationID
                                                    WHERE c.Id = @userId AND a.CreatedBy = @userId", new { userId })).ToList();
            }
        }

        public async Task<List<Domain.Entities.Apply>> GetOrganisationApplications(Guid userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection.QueryAsync<Domain.Entities.Apply>(@"SELECT a.* FROM Contacts c
                                                    INNER JOIN Apply a ON a.OrganisationId = c.ApplyOrganisationID
                                                    WHERE c.Id = @userId", new { userId })).ToList();
            }
        }

        public async Task<bool> CanSubmitApplication(Guid applicationId)
        {
            var canSubmit = false;

            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var application = await GetApplication(applicationId);
                var invalidApplicationStatuses = new List<string> { ApplicationStatus.Approved, ApplicationStatus.Rejected };

                // Application must exist and has not already been Approved or Rejected
                if (application != null && !invalidApplicationStatuses.Contains(application.ApplicationStatus))
                {
                    var otherAppsInProgress = await connection.QueryAsync<Domain.Entities.Apply>(@"
                                                        SELECT a.*
                                                        FROM Apply a
                                                        INNER JOIN Organisations o ON o.Id = a.OrganisationId
														INNER JOIN Contacts con ON a.OrganisationId = con.ApplyOrganisationID
                                                        WHERE a.OrganisationId = (SELECT OrganisationId FROM Apply WHERE ApplicationId = @applicationId)
														AND a.CreatedBy <> (SELECT CreatedBy FROM Apply WHERE ApplicationId = @applicationId)
                                                        AND a.ApplicationStatus NOT IN (@applicationStatusApproved, @applicationStatusApprovedRejected)",
                                                            new
                                                            {
                                                                applicationId,
                                                                applicationStatusApproved = ApplicationStatus.Approved,
                                                                applicationStatusApprovedRejected = ApplicationStatus.Rejected
                                                            });

                    canSubmit = !otherAppsInProgress.Any();
                }
            }

            return canSubmit;
        }

        public async Task SubmitApplication(Guid applicationId, ApplyData applyData, Guid submittedBy)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply
                                                SET  ApplicationStatus = @ApplicationStatus, ApplyData = @applyData, AssessorReviewStatus = @ReviewStatus, UpdatedBy = @submittedBy, UpdatedAt = GETUTCDATE() 
                                                WHERE  (Apply.ApplicationId = @applicationId)",
                                                new { applicationId, ApplicationStatus = ApplicationStatus.Submitted, applyData, ReviewStatus = "New", submittedBy });
            }
        }

        public async Task<bool> ChangeProviderRoute(Guid applicationId, int providerRoute)
        {
            var application = await GetApplication(applicationId);
            var applyData = application?.ApplyData;

            if (application != null && applyData?.ApplyDetails != null)
            {
                applyData.ApplyDetails.ProviderRoute = providerRoute;

                using (var connection = new SqlConnection(_config.SqlConnectionString))
                {
                    await connection.ExecuteAsync(@"UPDATE Apply
                                                    SET  ApplyData = JSON_MODIFY(ApplyData, '$.ApplyDetails.ProviderRoute', @providerRoute)
                                                    WHERE  ApplicationId = @ApplicationId",
                                                    new { application.ApplicationId, providerRoute });
                }

                return true;
            }

            return false;
        }
        
        public async Task<List<ApplicationSequence>> GetSequences(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
               return (await connection.QueryAsync<ApplicationSequence>(@"SELECT * FROM ApplicationSequences WHERE ApplicationId = @applicationId",
                    new {applicationId})).ToList();
            }
        }

        public async Task UpdateApplicationStatus(Guid applicationId, string status)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply
                                                SET  ApplicationStatus = @status                                                
                                                WHERE ApplicationId = @ApplicationId", new {applicationId, status});
            }
        }

        public async Task<List<ApplicationSummaryItem>> GetOpenApplications(int sequenceId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<ApplicationSummaryItem>(
                        @"SELECT OrganisationName, ApplicationId, SequenceId,
                            CASE WHEN SequenceId = 1 THEN 'Midpoint'
                                 WHEN SequenceId = 2 THEN 'Standard'
                                 ELSE 'Unknown'
                            END As ApplicationType,
                            StandardName,
                            StandardCode,
                            SubmittedDate,
                            SubmissionCount,
                            CASE WHEN SequenceId = 1 THEN Sec3Status
		                         ELSE NULL
	                        END As FinancialStatus,
                            CASE WHEN SequenceId = 1 THEN SelectedFinancialGrade
		                         ELSE NULL
	                        END As FinancialGrade,
	                        CASE WHEN (SequenceStatus = @sequenceStatusFeedbackAdded) THEN @sequenceStatusFeedbackAdded
                                 WHEN (SubmissionCount > 1 AND SequenceId = 1 AND Sec1Status = @sectionStatusSubmitted AND Sec2Status = @sectionStatusSubmitted) THEN @sequenceStatusResubmitted
                                 WHEN (SubmissionCount > 1 AND SequenceId = 1 AND Sec1Status = Sec2Status AND Sec3Status = @sectionStatusSubmitted) THEN @sequenceStatusResubmitted
                                 WHEN (SubmissionCount > 1 AND SequenceId = 2 AND Sec4Status = @sectionStatusSubmitted) THEN @sequenceStatusResubmitted
                                 WHEN (SubmissionCount > 1 AND RequestedFeedbackAnswered = 'true') THEN @sequenceStatusResubmitted
                                 WHEN (SequenceId = 1 AND Sec1Status = Sec2Status) THEN Sec1Status
		                         WHEN SequenceId = 2 THEN Sec4Status
		                         ELSE @sectionStatusInProgress
	                        END As CurrentStatus
                        FROM (
	                        SELECT 
                                org.Name AS OrganisationName,
                                appl.id AS ApplicationId,
                                seq.SequenceId AS SequenceId,
                                CASE WHEN seq.SequenceId = 1 THEN NULL
		                             ELSE JSON_VALUE(appl.ApplicationData, '$.StandardName')
                                END As StandardName,
                                CASE WHEN seq.SequenceId = 1 THEN NULL
		                             ELSE JSON_VALUE(appl.ApplicationData, '$.StandardCode')
                                END As StandardCode,
                                CASE WHEN seq.SequenceId = 1 THEN JSON_VALUE(appl.ApplicationData, '$.LatestInitSubmissionDate')
		                             WHEN seq.SequenceId = 2 THEN JSON_VALUE(appl.ApplicationData, '$.LatestStandardSubmissionDate')
		                             ELSE NULL
	                            END As SubmittedDate,
                                CASE WHEN seq.SequenceId = 1 THEN JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount')
		                             WHEN seq.SequenceId = 2 THEN JSON_VALUE(appl.ApplicationData, '$.StandardSubmissionsCount')
		                             ELSE 0
	                            END As SubmissionCount,
                                MAX(CASE WHEN sec.[SectionId] = 3 THEN JSON_VALUE(sec.QnAData, '$.FinancialApplicationGrade.SelectedGrade') ELSE NULL END) AS SelectedFinancialGrade,
                                seq.Status AS SequenceStatus,
	                            MAX(CASE WHEN sec.[SectionId] = 1 THEN sec.[Status] ELSE NULL END) AS Sec1Status,
	                            MAX(CASE WHEN sec.[SectionId] = 2 THEN sec.[Status] ELSE NULL END) AS Sec2Status,
	                            MAX(CASE WHEN sec.[SectionId] = 3 THEN sec.[Status] ELSE NULL END) AS Sec3Status,
	                            MAX(CASE WHEN sec.[SectionId] = 4 THEN sec.[Status] ELSE NULL END) AS Sec4Status,
                                MAX(JSON_VALUE(sec.QnAData, '$.RequestedFeedbackAnswered')) AS RequestedFeedbackAnswered
	                        FROM Applications appl
	                        INNER JOIN ApplicationSequences seq ON seq.ApplicationId = appl.Id
	                        INNER JOIN ApplicationSections sec ON sec.ApplicationId = appl.Id 
	                        INNER JOIN Organisations org ON org.Id = appl.ApplyingOrganisationId
	                        WHERE appl.ApplicationStatus = @applicationStatusSubmitted
                                AND seq.SequenceId = @sequenceId
                                AND seq.Status = @sequenceStatusSubmitted
                                AND seq.IsActive = 1
	                        GROUP BY seq.SequenceId, seq.Status, appl.ApplyingOrganisationId, appl.id, org.Name, appl.ApplicationData 
                        ) ab",
                        new
                        {
                            sequenceId,
                            applicationStatusSubmitted = ApplicationStatus.Submitted,
                            sequenceStatusSubmitted = ApplicationSequenceStatus.Submitted,
                            sequenceStatusResubmitted = ApplicationSequenceStatus.Resubmitted,
                            sequenceStatusFeedbackAdded = ApplicationSequenceStatus.FeedbackAdded,
                            sectionStatusSubmitted = ApplicationSectionStatus.Submitted,
                            sectionStatusInProgress = ApplicationSectionStatus.InProgress   
                        })).ToList();
            }
        }

        public async Task<List<ApplicationSummaryItem>> GetFeedbackAddedApplications()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<ApplicationSummaryItem>(
                        @"SELECT OrganisationName, ApplicationId, SequenceId,
                            CASE WHEN SequenceId = 1 THEN 'Midpoint'
                                 WHEN SequenceId = 2 THEN 'Standard'
                                 ELSE 'Unknown'
                            END As ApplicationType,
                            StandardName,
                            StandardCode,
                            FeedbackAddedDate,
                            SubmissionCount,
                            SequenceStatus AS CurrentStatus
                        FROM (
	                        SELECT 
                                org.Name AS OrganisationName,
                                appl.id AS ApplicationId,
                                seq.SequenceId AS SequenceId,
                                CASE WHEN seq.SequenceId = 1 THEN NULL
		                             ELSE JSON_VALUE(appl.ApplicationData, '$.StandardName')
                                END As StandardName,
                                CASE WHEN seq.SequenceId = 1 THEN NULL
		                             ELSE JSON_VALUE(appl.ApplicationData, '$.StandardCode')
                                END As StandardCode,
                                CASE WHEN seq.SequenceId = 1 THEN JSON_VALUE(appl.ApplicationData, '$.InitSubmissionFeedbackAddedDate')
		                             WHEN seq.SequenceId = 2 THEN JSON_VALUE(appl.ApplicationData, '$.StandardSubmissionFeedbackAddedDate')
		                             ELSE NULL
	                            END As FeedbackAddedDate,
                                CASE WHEN seq.SequenceId = 1 THEN JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount')
		                             WHEN seq.SequenceId = 2 THEN JSON_VALUE(appl.ApplicationData, '$.StandardSubmissionsCount')
		                             ELSE 0
	                            END As SubmissionCount,
                                seq.Status AS SequenceStatus
	                        FROM Applications appl
	                        INNER JOIN ApplicationSequences seq ON seq.ApplicationId = appl.Id
	                        INNER JOIN Organisations org ON org.Id = appl.ApplyingOrganisationId
	                        WHERE appl.ApplicationStatus = @applicationStatusFeedbackAdded
                                AND seq.Status = @sequenceStatusFeedbackAdded
                                AND seq.IsActive = 1
	                        GROUP BY seq.SequenceId, seq.Status, appl.ApplyingOrganisationId, appl.id, org.Name, appl.ApplicationData 
                        ) ab",
                        new
                        {
                            applicationStatusFeedbackAdded = ApplicationStatus.FeedbackAdded,
                            sequenceStatusFeedbackAdded = ApplicationSequenceStatus.FeedbackAdded
                        })).ToList();
            }
        }

        public async Task<List<ApplicationSummaryItem>> GetClosedApplications()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<ApplicationSummaryItem>(
                        @"SELECT OrganisationName, ApplicationId, SequenceId,
                            CASE WHEN SequenceId = 1 THEN 'Midpoint'
                                 WHEN SequenceId = 2 THEN 'Standard'
                                 ELSE 'Unknown'
                            END As ApplicationType,
                            StandardName,
                            StandardCode,
                            ClosedDate,
                            SubmissionCount,
                            SequenceStatus As CurrentStatus
                        FROM (
	                        SELECT 
                                org.Name AS OrganisationName,
                                appl.id AS ApplicationId,
                                seq.SequenceId AS SequenceId,
                            CASE WHEN seq.SequenceId = 1 THEN NULL
		                            ELSE JSON_VALUE(appl.ApplicationData, '$.StandardName')
                            END As StandardName,
                            CASE WHEN seq.SequenceId = 1 THEN NULL
		                            ELSE JSON_VALUE(appl.ApplicationData, '$.StandardCode')
                            END As StandardCode,
                            CASE WHEN seq.SequenceId = 1 THEN JSON_VALUE(appl.ApplicationData, '$.InitSubmissionClosedDate')
		                         WHEN seq.SequenceId = 2 THEN JSON_VALUE(appl.ApplicationData, '$.StandardSubmissionClosedDate')
		                         ELSE NULL
	                        END As ClosedDate,
                            CASE WHEN seq.SequenceId = 1 THEN JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount')
		                         WHEN seq.SequenceId = 2 THEN JSON_VALUE(appl.ApplicationData, '$.StandardSubmissionsCount')
		                         ELSE 0
	                        END As SubmissionCount,
                                seq.Status As SequenceStatus
	                        FROM Applications appl
	                        INNER JOIN ApplicationSequences seq ON seq.ApplicationId = appl.Id
	                        INNER JOIN Organisations org ON org.Id = appl.ApplyingOrganisationId
	                        WHERE seq.Status IN (@sequenceStatusApproved, @sequenceStatusRejected) AND seq.NotRequired = 0 AND appl.DeletedAt IS NULL
	                        GROUP BY seq.SequenceId, seq.Status, appl.ApplyingOrganisationId, appl.id, org.Name, appl.ApplicationData 
                        ) ab",
                        new
                        {
                            sequenceStatusApproved = ApplicationSequenceStatus.Approved,
                            sequenceStatusRejected = ApplicationSequenceStatus.Rejected
                        })).ToList();
            }
        }

        public async Task<List<FinancialApplicationSummaryItem>> GetOpenFinancialApplications()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<FinancialApplicationSummaryItem>(
                        @"SELECT 
                            org.Name AS OrganisationName,
                            appl.id AS ApplicationId,
                            seq.SequenceId AS SequenceId,
                            sec.SectionId AS SectionId,
                            JSON_VALUE(appl.ApplicationData, '$.LatestInitSubmissionDate') As SubmittedDate,
                            JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount') As SubmissionCount,
	                        CASE WHEN (seq.Status = @sequenceStatusFeedbackAdded) THEN @sequenceStatusFeedbackAdded
                                 WHEN (JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount') > 1 AND sec.Status = @financialStatusSubmitted) THEN @sequenceStatusResubmitted
                                 WHEN (JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount') > 1 AND JSON_VALUE(sec.QnAData, '$.RequestedFeedbackAnswered') = 'true') THEN @sequenceStatusResubmitted
                                 ELSE sec.Status
	                        END As CurrentStatus
	                      FROM Applications appl
	                      INNER JOIN ApplicationSequences seq ON seq.ApplicationId = appl.Id
	                      INNER JOIN ApplicationSections sec ON sec.ApplicationId = appl.Id
	                      INNER JOIN Organisations org ON org.Id = appl.ApplyingOrganisationId
	                      WHERE seq.SequenceId = 1 AND sec.SectionId = 3 AND seq.IsActive = 1
                            AND appl.ApplicationStatus = @applicationStatusSubmitted
                            AND seq.Status = @sequenceStatusSubmitted
                            AND sec.Status IN (@financialStatusSubmitted, @financialStatusInProgress)",
                        new
                        {
                            applicationStatusSubmitted = ApplicationStatus.Submitted,
                            sequenceStatusSubmitted = ApplicationSequenceStatus.Submitted,
                            sequenceStatusFeedbackAdded = ApplicationSequenceStatus.FeedbackAdded,
                            sequenceStatusResubmitted = ApplicationSequenceStatus.Resubmitted,
                            financialStatusSubmitted = ApplicationSectionStatus.Submitted,
                            financialStatusInProgress = ApplicationSectionStatus.InProgress
                        })).ToList();
            }
        }

        public async Task<List<FinancialApplicationSummaryItem>> GetFeedbackAddedFinancialApplications()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<FinancialApplicationSummaryItem>(
                        @"SELECT 
                            org.Name AS OrganisationName,
                            appl.id AS ApplicationId,
                            seq.SequenceId AS SequenceId,
                            sec.SectionId AS SectionId,
                            JSON_QUERY(sec.QnAData, '$.FinancialApplicationGrade') AS Grade,
                            ISNULL(JSON_VALUE(appl.ApplicationData, '$.InitSubmissionFeedbackAddedDate'),
								   JSON_VALUE(sec.QnAData, '$.FinancialApplicationGrade.GradedDateTime')) As FeedbackAddedDate,
                            JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount') As SubmissionCount,
	                        seq.Status As CurrentStatus
	                      FROM Applications appl
	                      INNER JOIN ApplicationSequences seq ON seq.ApplicationId = appl.Id
	                      INNER JOIN ApplicationSections sec ON sec.ApplicationId = appl.Id
	                      INNER JOIN Organisations org ON org.Id = appl.ApplyingOrganisationId
	                      WHERE seq.SequenceId = 1 AND sec.SectionId = 3 AND seq.IsActive = 1
                            AND (
                                    seq.Status = @sequenceStatusFeedbackAdded
                                    OR ( 
                                            sec.Status IN (@financialStatusGraded, @financialStatusEvaluated)
                                            AND JSON_VALUE(sec.QnAData, '$.FinancialApplicationGrade.SelectedGrade') = @selectedGradeInadequate
                                        )
                                )",
                        new
                        {
                            sequenceStatusFeedbackAdded = ApplicationSequenceStatus.FeedbackAdded,
                            financialStatusGraded = ApplicationSectionStatus.Graded,
                            financialStatusEvaluated = ApplicationSectionStatus.Evaluated,
                            selectedGradeInadequate = FinancialApplicationSelectedGrade.Inadequate
                        })).ToList();
            }
        }

        public async Task<List<FinancialApplicationSummaryItem>> GetClosedFinancialApplications()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<FinancialApplicationSummaryItem>(
                        @"SELECT 
                            org.Name AS OrganisationName,
                            appl.id AS ApplicationId,
                            seq.SequenceId AS SequenceId,
                            sec.SectionId AS SectionId,
                            JSON_QUERY(sec.QnAData, '$.FinancialApplicationGrade') AS Grade,
                            JSON_VALUE(appl.ApplicationData, '$.InitSubmissionClosedDate') As ClosedDate,
                            JSON_VALUE(appl.ApplicationData, '$.InitSubmissionsCount') As SubmissionCount,
	                        CASE WHEN (seq.Status = @sequenceStatusApproved) THEN @sequenceStatusApproved
                                 WHEN (seq.Status = @sequenceStatusRejected) THEN @sequenceStatusRejected
                                 ELSE sec.Status
	                        END As CurrentStatus
	                      FROM Applications appl
	                      INNER JOIN ApplicationSequences seq ON seq.ApplicationId = appl.Id
	                      INNER JOIN ApplicationSections sec ON sec.ApplicationId = appl.Id
	                      INNER JOIN Organisations org ON org.Id = appl.ApplyingOrganisationId
	                      WHERE seq.SequenceId = 1 AND sec.SectionId = 3 AND seq.NotRequired = 0 AND appl.DeletedAt IS NULL
	                        AND JSON_QUERY(sec.QnAData, '$.FinancialApplicationGrade') IS NOT NULL						  
                            AND (
                                    seq.Status IN (@sequenceStatusApproved, @sequenceStatusRejected)
                                    OR ( 
                                            sec.Status IN (@financialStatusGraded, @financialStatusEvaluated)
                                            AND JSON_VALUE(sec.QnAData, '$.FinancialApplicationGrade.SelectedGrade') <> @selectedGradeInadequate
                                        )
                                )",
                        new
                        {
                            sequenceStatusApproved = ApplicationSequenceStatus.Approved,
                            sequenceStatusRejected = ApplicationSequenceStatus.Rejected,
                            financialStatusGraded = ApplicationSectionStatus.Graded,
                            financialStatusEvaluated = ApplicationSectionStatus.Evaluated,
                            selectedGradeInadequate = FinancialApplicationSelectedGrade.Inadequate
                        })).ToList();
            }
        }

        public async Task<Organisation> GetOrganisationForApplication(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleAsync<Organisation>(@"SELECT org.* FROM Organisations org 
                                                                        INNER JOIN Applications appl ON appl.ApplyingOrganisationId = org.Id
                                                                        WHERE appl.Id = @ApplicationId",
                    new {applicationId});
            }
        }

        public async Task<string> CheckOrganisationStandardStatus(Guid applicationId, int standardId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
               var applicationStatuses= await connection.QueryAsync<string>(@"select top 1 A.applicationStatus from Applications A
                                                                    where JSON_VALUE(ApplicationData,'$.StandardCode')= @standardId
                                                                    and ApplyingOrganisationId in 
                                                                        (select ApplyingOrganisationId from Applications where Id = @applicationId)
",
                    new { applicationId, standardId });

                return !applicationStatuses.Any() ? string.Empty : applicationStatuses.FirstOrDefault();
            }
        }

        public async Task<IEnumerable<RoatpApplicationStatus>> GetExistingApplicationStatusByUkprn(string ukprn)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var applicationStatuses = await connection.QueryAsync<RoatpApplicationStatus>(
                    @"select a.Id AS ApplicationId, a.ApplicationStatus AS Status
                      from dbo.Apply a
                      where JSON_VALUE(ApplyData, '$.ApplyDetails.UKPRN') = @ukprn",
                 new { ukprn });

                return await Task.FromResult(applicationStatuses);
            }
        }

        public async Task<string> GetNextRoatpApplicationReference()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var nextInSequence = (await connection.QueryAsync<int>(@"SELECT NEXT VALUE FOR RoatpAppReferenceSequence")).FirstOrDefault();

                return $"APR{nextInSequence}";
            }
        }
    }
}

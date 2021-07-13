using Dapper;
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
using SFA.DAS.ApplyService.Domain.Interfaces;
using SFA.DAS.ApplyService.Types;

namespace SFA.DAS.ApplyService.Data
{
    public class ApplyRepository : IApplyRepository
    {
        private readonly IApplyConfig _config;

        public ApplyRepository(IConfigurationService configurationService)
        {
            _config = configurationService.GetConfig().Result;
            
            SqlMapper.AddTypeHandler(typeof(ApplyData), new ApplyDataHandler());
            SqlMapper.AddTypeHandler(typeof(OrganisationDetails), new OrganisationDetailsHandler());
            SqlMapper.AddTypeHandler(typeof(QnAData), new QnADataHandler());
            SqlMapper.AddTypeHandler(typeof(FinancialReviewDetails), new FinancialReviewDetailsDataHandler());
        }

        public async Task<Guid> StartApplication(Guid applicationId, ApplyData applyData, Guid organisationId, Guid createdBy)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleAsync<Guid>(
                    @"INSERT INTO Apply (ApplicationId, OrganisationId, ApplicationStatus, ApplyData, AssessorReviewStatus, GatewayReviewStatus, FinancialReviewStatus, CreatedBy, CreatedAt)
                                        OUTPUT INSERTED.[ApplicationId] 
                                        VALUES (@applicationId, @organisationId, @applicationStatus, @applyData, @assessorReviewStatus, @gatewayReviewStatus, @financialReviewStatus, @createdBy, GETUTCDATE())",
                    new { applicationId, organisationId, applicationStatus = ApplicationStatus.InProgress, applyData, assessorReviewStatus = AssessorReviewStatus.Draft, gatewayReviewStatus = GatewayReviewStatus.Draft, financialReviewStatus = FinancialReviewStatus.Draft, createdBy });
            }
        }

        public async Task<Apply> GetApplication(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleOrDefaultAsync<Apply>(
                    @"SELECT * FROM apply WHERE ApplicationId = @applicationId",
                    new { applicationId });
            }
        }

        public async Task<Apply> GetApplicationByUserId(Guid applicationId, Guid signinId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection.QuerySingleOrDefaultAsync<Apply>(@"SELECT top 1 a.* FROM Contacts c
                                                    INNER JOIN Apply a ON a.OrganisationId = c.ApplyOrganisationID
                                                    WHERE c.SigninId = @signinId and a.ApplicationId = @ApplicationId", new { signinId, applicationId }));
            }
        }

        public async Task<List<Apply>> GetUserApplications(Guid signinId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection.QueryAsync<Apply>(@"SELECT a.* FROM Contacts c
                                                    INNER JOIN Apply a ON a.OrganisationId = c.ApplyOrganisationID
                                                    WHERE c.SigninId = @signinId AND a.CreatedBy = c.Id", new { signinId })).ToList();
            }
        }

        public async Task<List<Apply>> GetOrganisationApplications(Guid signinId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection.QueryAsync<Apply>(@"SELECT a.* FROM Contacts c
                                                    INNER JOIN Apply a ON a.OrganisationId = c.ApplyOrganisationID
                                                    WHERE c.SigninId = @signinId", new { signinId })).ToList();
            }
        }

        public async Task UpdateApplication(Apply application)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(
                    @"UPDATE [Apply] SET
                        ApplicationStatus = @ApplicationStatus,
                        GatewayReviewStatus = @GatewayReviewStatus,
                        AssessorReviewStatus = @AssessorReviewStatus,
                        FinancialReviewStatus = @FinancialReviewStatus,
                        FinancialGrade = @FinancialGrade,
                        Assessor1UserId = @Assessor1UserId,
                        Assessor2UserId = @Assessor2UserId,
                        Assessor1Name = @Assessor1Name,
                        Assessor2Name = @Assessor2Name,
                        Assessor1ReviewStatus = @Assessor1ReviewStatus,
                        Assessor2ReviewStatus = @Assessor2ReviewStatus,
                        ModerationStatus = @ModerationStatus,
                        GatewayUserId = @gatewayUserId,
                        GatewayUserName = @gatewayUserName,
                        UpdatedBy = @updatedBy,
                        UpdatedAt = @updatedAt
                        WHERE [Id] = @id",
                    application);
            }
        }

        public async Task<bool> CanSubmitApplication(Guid applicationId)
        {
            var canSubmit = false;

            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var application = await GetApplication(applicationId);

                var invalidApplicationStatuses = new List<string> { ApplicationStatus.Successful, ApplicationStatus.Rejected, ApplicationStatus.Unsuccessful, ApplicationStatus.Removed, ApplicationStatus.Withdrawn, ApplicationStatus.Cancelled };

                // Application must exist and has not already been Approved, Rejected, Removed, Widthdrawn or Cancelled
                if (application != null && !invalidApplicationStatuses.Contains(application.ApplicationStatus))
                {
                    var otherAppsInProgress = await connection.QueryAsync<Domain.Entities.Apply>(@"
                                                        SELECT a.*
                                                        FROM Apply a
                                                        INNER JOIN Organisations o ON o.Id = a.OrganisationId
														INNER JOIN Contacts con ON a.OrganisationId = con.ApplyOrganisationID
                                                        WHERE a.OrganisationId = (SELECT OrganisationId FROM Apply WHERE ApplicationId = @applicationId)
														AND a.CreatedBy <> (SELECT CreatedBy FROM Apply WHERE ApplicationId = @applicationId)
                                                        AND a.ApplicationStatus NOT IN (@applicationStatusSuccessful, @applicationStatusUnsuccessful, @applicationStatusRejected, @applicationStatusRemoved, @applicationStatusWithdrawn, @applicationStatusCancelled)",
                                                            new
                                                            {
                                                                applicationId,
                                                                applicationStatusSuccessful = ApplicationStatus.Successful,   
                                                                applicationStatusUnsuccessful = ApplicationStatus.Unsuccessful,
                                                                applicationStatusRejected = ApplicationStatus.Rejected,   
                                                                applicationStatusRemoved = ApplicationStatus.Removed,
                                                                applicationStatusWithdrawn = ApplicationStatus.Withdrawn,
                                                                applicationStatusCancelled = ApplicationStatus.Cancelled
                                                            });

                    canSubmit = !otherAppsInProgress.Any();
                }
            }

            return canSubmit;
        }

        public async Task<bool> SubmitApplication(Guid applicationId, ApplyData applyData, FinancialData financialData, Guid submittedBy)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.OpenAsync();
                var rowsAffected = await connection.ExecuteAsync(@"UPDATE Apply
                                                SET ApplicationStatus = @ApplicationStatus, 
                                                    ApplyData = @applyData, 
                                                    AssessorReviewStatus = @AssessorReviewStatus, 
                                                    GatewayReviewStatus = @GatewayReviewStatus, 
                                                    FinancialReviewStatus = @FinancialReviewStatus,
                                                    UpdatedBy = @submittedBy, 
                                                    UpdatedAt = GETUTCDATE() 
                                                WHERE  (Apply.ApplicationId = @applicationId)",
                                                new { applicationId, 
                                                      ApplicationStatus = ApplicationStatus.Submitted, 
                                                      applyData,
                                                      AssessorReviewStatus = AssessorReviewStatus.New, 
                                                      GatewayReviewStatus = GatewayReviewStatus.New, 
                                                      FinancialReviewStatus = FinancialReviewStatus.New,
                                                      submittedBy });

                await connection.ExecuteAsync(@"insert into FinancialData ([ApplicationId]
               ,[TurnOver],[Depreciation],[ProfitLoss],[Dividends],[IntangibleAssets]
               ,[Assets],[Liabilities],[ShareholderFunds],[Borrowings],[AccountingReferenceDate],[AccountingPeriod],[AverageNumberofFTEEmployees])
                values (@ApplicationId, @TurnOver,@Depreciation, @ProfitLoss,@Dividends,@IntangibleAssets
               ,@Assets,@Liabilities,@ShareholderFunds,@Borrowings,@AccountingReferenceDate,@AccountingPeriod,@AverageNumberofFTEEmployees)",
               financialData);

                connection.Close();

                return rowsAffected > 0;
            }
        }

        public async Task<Guid> SnapshotApplication(Guid applicationId, Guid snapshotApplicationId, List<ApplySequence> newSequences)
        {
            var currentApplication = await GetApplication(applicationId);

            if (currentApplication != null)
            {
                var newApplyData = new ApplyData
                {
                    Sequences = newSequences,
                    ApplyDetails = currentApplication.ApplyData?.ApplyDetails
                };

                using (var connection = new SqlConnection(_config.SqlConnectionString))
                {
                    return await connection.QuerySingleAsync<Guid>(
                        @"INSERT INTO ApplySnapshots (ApplicationId, SnapshotApplicationId, SnapshotDate, OrganisationId, ApplicationStatus, ApplyData, GatewayReviewStatus, AssessorReviewStatus, FinancialReviewStatus, FinancialGrade)
                          OUTPUT INSERTED.[SnapshotApplicationId] 
                          VALUES (@ApplicationId, @snapshotApplicationId, GETUTCDATE(), @OrganisationId, @ApplicationStatus, @newApplyData, @GatewayReviewStatus, @AssessorReviewStatus, @FinancialReviewStatus, @FinancialGrade)",
                        new
                        {
                            currentApplication.ApplicationId,
                            snapshotApplicationId,
                            currentApplication.OrganisationId,
                            currentApplication.ApplicationStatus,
                            newApplyData,
                            currentApplication.GatewayReviewStatus,
                            currentApplication.AssessorReviewStatus,
                            currentApplication.FinancialReviewStatus,
                            currentApplication.FinancialGrade
                        });
                }
            }

            return Guid.Empty;
        }      

        public async Task<bool> ChangeProviderRoute(Guid applicationId, int providerRoute, string providerRouteName)
        {
            var application = await GetApplication(applicationId);
            var applyData = application?.ApplyData;

            if (application != null && applyData?.ApplyDetails != null)
            {
                applyData.ApplyDetails.ProviderRoute = providerRoute;
                applyData.ApplyDetails.ProviderRouteName = providerRouteName;

                using (var connection = new SqlConnection(_config.SqlConnectionString))
                {
                    await connection.ExecuteAsync(@"UPDATE Apply
                                                    SET  ApplyData = @applyData
                                                    WHERE  ApplicationId = @ApplicationId",
                                                    new { application.ApplicationId, applyData });
                }

                return true;
            }

            return false;
        }
        


        public async Task<List<RoatpFinancialSummaryItem>> GetOpenFinancialApplications(string sortOrder, string sortColumn)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var orderByClause = $"{GetSortColumnForNew(sortColumn)} { GetOrderByDirectionFinancial(sortOrder)}";

                return (await connection
                    .QueryAsync<RoatpFinancialSummaryItem>(
                        $@"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
                            apply.ApplicationStatus AS ApplicationStatus,
                            apply.GatewayReviewStatus AS GatewayReviewStatus,
                            apply.AssessorReviewStatus AS AssessorReviewStatus,
                            apply.FinancialReviewStatus AS FinancialReviewStatus,
                            org.Name AS OrganisationName,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.UKPRN') AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ApplicationRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.OutcomeDateTime') AS GatewayOutcomeDate,
                            CASE seq.NotRequired WHEN 'false' THEN 'Not exempt' ELSE 'Exempt' END AS DeclaredInApplication
	                      FROM Apply apply
	                      INNER JOIN Organisations org ON org.Id = apply.OrganisationId	                      
                        CROSS APPLY OPENJSON(apply.ApplyData)
                        WITH (
                            Sequences nvarchar(max) '$.Sequences' AS JSON
                        ) AS i
                        CROSS APPLY (
                            SELECT *
                            FROM OPENJSON(i.Sequences)
                            WITH (
                                [SequenceNo] nvarchar(max) '$.SequenceNo',
                                [NotRequired] nvarchar(max) '$.NotRequired'
                            )
                        ) seq
                        WHERE seq.SequenceNo = @financialHealthSequence
                          AND apply.GatewayReviewStatus IN (@gatewayStatusPass) -- NOTE: If Gateway did not pass then it goes straight to Oversight
                          AND apply.ApplicationStatus = @applicationStatusGatewayAssessed AND apply.DeletedAt IS NULL
                          AND apply.FinancialReviewStatus IN (@financialStatusDraft, @financialStatusNew, @financialStatusInProgress)
                        ORDER BY {orderByClause}, org.Name ASC",
                        new
                        {
                            financialHealthSequence = 2,
                            gatewayStatusPass = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            financialStatusDraft = FinancialReviewStatus.Draft,
                            financialStatusNew = FinancialReviewStatus.New,
                            financialStatusInProgress = FinancialReviewStatus.InProgress
                        })).ToList();
            }
        }

        public async Task<List<RoatpFinancialSummaryDownloadItem>> GetOpenFinancialApplicationsForDownload()
        {
            var sql = @"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
                            apply.ApplicationStatus AS ApplicationStatus,
                            apply.GatewayReviewStatus AS GatewayReviewStatus,
                            apply.AssessorReviewStatus AS AssessorReviewStatus,
                            apply.FinancialReviewStatus AS FinancialReviewStatus,
                            org.Name AS OrganisationName,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.UKPRN') AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ApplicationRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.OutcomeDateTime') AS GatewayOutcomeDate,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.CompaniesHouseDetails.CompanyNumber') AS CompanyNumber,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.CharityCommissionDetails.CharityNumber') AS CharityNumber,
                            CASE seq.NotRequired WHEN 'false' THEN 'Not exempt' ELSE 'Exempt' END AS DeclaredInApplication,
                            fd.ApplicationId,
                            fd.TurnOver,
                            fd.Depreciation,
                            fd.ProfitLoss,
                            fd.Dividends,
                            fd.IntangibleAssets,
                            fd.Assets,
                            fd.Liabilities,
                            fd.ShareholderFunds,
                            fd.Borrowings,
                            fd.AccountingReferenceDate,
                            fd.AccountingPeriod,
                            fd.AverageNumberofFTEEmployees
	                      FROM Apply apply
                          LEFT JOIN FinancialData fd on fd.ApplicationId = apply.ApplicationId
	                      INNER JOIN Organisations org ON org.Id = apply.OrganisationId	                      
                        CROSS APPLY OPENJSON(apply.ApplyData)
                        WITH (
                            Sequences nvarchar(max) '$.Sequences' AS JSON
                        ) AS i
                        CROSS APPLY (
                            SELECT *
                            FROM OPENJSON(i.Sequences)
                            WITH (
                                [SequenceNo] nvarchar(max) '$.SequenceNo',
                                [NotRequired] nvarchar(max) '$.NotRequired'
                            )
                        ) seq
                        WHERE seq.SequenceNo = @financialHealthSequence
                          AND apply.ApplicationStatus = @applicationStatusGatewayAssessed AND apply.DeletedAt IS NULL
                          AND apply.FinancialReviewStatus IN ( @financialStatusDraft, @financialStatusNew, @financialStatusInProgress)
                          AND apply.GatewayReviewStatus IN (@gatewayStatusPass)
                        ORDER BY CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ASC, org.Name ASC";

            var parameters = new
            {
                financialHealthSequence = 2,
                applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                financialStatusDraft = FinancialReviewStatus.Draft,
                financialStatusNew = FinancialReviewStatus.New,
                financialStatusInProgress = FinancialReviewStatus.InProgress,
                gatewayStatusPass = GatewayReviewStatus.Pass
            };

            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var results = await connection
                    .QueryAsync<RoatpFinancialSummaryDownloadItem, FinancialData, RoatpFinancialSummaryDownloadItem>(
                        sql, ((item, data) =>
                            {
                                item.FinancialData = data;
                                return item;
                            }
                        ), parameters, null, true, "ApplicationId");

                return results.ToList();
            }
        }

        public async Task<List<RoatpFinancialSummaryItem>> GetClarificationFinancialApplications(string sortOrder, string sortColumn)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var orderByClause = $"{GetSortColumnForNew(sortColumn)} { GetOrderByDirectionFinancial(sortOrder)}";
                return (await connection
                    .QueryAsync<RoatpFinancialSummaryItem>(
                        $@"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
                            apply.ApplicationStatus AS ApplicationStatus,
                            apply.GatewayReviewStatus AS GatewayReviewStatus,
                            apply.AssessorReviewStatus AS AssessorReviewStatus,
                            apply.FinancialReviewStatus AS FinancialReviewStatus,
                            org.Name AS OrganisationName,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.UKPRN') AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ApplicationRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate,
                            JSON_VALUE(apply.FinancialGrade, '$.ClarificationRequestedOn') AS ClarificationRequestedDate,
                            JSON_VALUE(apply.FinancialGrade, '$.ClarificationRequestedBy') AS OutcomeMadeBy,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.OutcomeDateTime') AS GatewayOutcomeDate,
                            CASE seq.NotRequired WHEN 'false' THEN 'Not exempt' ELSE 'Exempt' END AS DeclaredInApplication
	                      FROM Apply apply
	                      INNER JOIN Organisations org ON org.Id = apply.OrganisationId	                      
                        CROSS APPLY OPENJSON(apply.ApplyData)
                        WITH (
                            Sequences nvarchar(max) '$.Sequences' AS JSON
                        ) AS i
                        CROSS APPLY (
                            SELECT *
                            FROM OPENJSON(i.Sequences)
                            WITH (
                                [SequenceNo] nvarchar(max) '$.SequenceNo',
                                [NotRequired] nvarchar(max) '$.NotRequired'
                            )
                        ) seq
                        WHERE seq.SequenceNo = @financialHealthSequence
                          AND apply.GatewayReviewStatus IN (@gatewayStatusPass) -- NOTE: If Gateway did not pass then it goes straight to Oversight
                          AND apply.ApplicationStatus = @applicationStatusGatewayAssessed AND apply.DeletedAt IS NULL
                          AND apply.FinancialReviewStatus IN (@financialStatusClarificationSent)
                       ORDER BY {orderByClause}, org.Name ASC",
                        new
                        {
                            financialHealthSequence = 2,
                            gatewayStatusPass = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            financialStatusClarificationSent = FinancialReviewStatus.ClarificationSent
                        })).ToList();
            }
        }

        public async Task<List<RoatpFinancialSummaryItem>> GetClosedFinancialApplications(string sortOrder, string sortColumn)
        {
            if (sortColumn == null)
                sortColumn = "OutcomeMadeDate";

            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var orderByClause = $"{GetSortColumnForNew(sortColumn)} { GetOrderByDirectionFinancial(sortOrder)}";
                return (await connection
                   .QueryAsync<RoatpFinancialSummaryItem>(
                       $@"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
                            apply.ApplicationStatus AS ApplicationStatus,
                            apply.GatewayReviewStatus AS GatewayReviewStatus,
                            apply.AssessorReviewStatus AS AssessorReviewStatus,
                            apply.FinancialReviewStatus AS FinancialReviewStatus,
                            org.Name AS OrganisationName,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.UKPRN') AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ApplicationRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate,
                            CASE 
                                WHEN apply.ApplicationStatus = @applicationStatusWithdrawn THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnOn')
                                WHEN apply.ApplicationStatus = @applicationStatusRemoved THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedOn')
                                ELSE JSON_VALUE(apply.FinancialGrade, '$.GradedDateTime')
                            END AS OutcomeMadeDate,
                            CASE 
                                WHEN apply.ApplicationStatus = @applicationStatusWithdrawn THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnBy')
                                WHEN apply.ApplicationStatus = @applicationStatusRemoved THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedBy')
                                ELSE JSON_VALUE(apply.FinancialGrade, '$.GradedBy')
                            END AS OutcomeMadeBy,
                            JSON_VALUE(apply.FinancialGrade, '$.SelectedGrade') AS SelectedGrade,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.OutcomeDateTime') AS GatewayOutcomeDate,
                            CASE seq.NotRequired WHEN 'false' THEN 'Not exempt' ELSE 'Exempt' END AS DeclaredInApplication
	                      FROM Apply apply
	                      INNER JOIN Organisations org ON org.Id = apply.OrganisationId	
                        CROSS APPLY OPENJSON(apply.ApplyData)
                        WITH (
                            Sequences nvarchar(max) '$.Sequences' AS JSON
                        ) AS i
                        CROSS APPLY (
                            SELECT *
                            FROM OPENJSON(i.Sequences)
                            WITH (
                                [SequenceNo] nvarchar(max) '$.SequenceNo',
                                [NotRequired] nvarchar(max) '$.NotRequired'
                            )
                        ) seq
                        WHERE seq.SequenceNo = @financialHealthSequence
                          AND apply.GatewayReviewStatus IN (@gatewayStatusPass) -- NOTE: If Gateway did not pass then it goes straight to Oversight
                          AND apply.DeletedAt IS NULL
                          AND (
                               apply.ApplicationStatus IN (@applicationStatusWithdrawn, @applicationStatusRemoved)
                               OR apply.FinancialReviewStatus IN (@financialStatusApproved, @financialStatusDeclined, @financialStatusExempt)
                              )
                        ORDER BY {orderByClause}, org.Name ASC",
                       new
                       {
                           financialHealthSequence = 2,
                           gatewayStatusPass = GatewayReviewStatus.Pass,
                           applicationStatusWithdrawn = ApplicationStatus.Withdrawn,
                           applicationStatusRemoved = ApplicationStatus.Removed,
                           financialStatusApproved = FinancialReviewStatus.Pass,
                           financialStatusDeclined = FinancialReviewStatus.Fail,
                           financialStatusExempt = FinancialReviewStatus.Exempt
                       })).ToList();
            }
        }

        public async Task<RoatpFinancialApplicationsStatusCounts> GetFinancialApplicationsStatusCounts()
        {
            // Note: For now it is easier to run all three queries. It may make sense to do something similar to that done with EPAO
            var openApplications = await GetOpenFinancialApplications(null,null);
            var clarificationApplications = await GetClarificationFinancialApplications(null,null);
            var closedApplications = await GetClosedFinancialApplications(null,null);

            return new RoatpFinancialApplicationsStatusCounts
            {
                ApplicationsOpen = openApplications.Count,
                ApplicationsWithClarification = clarificationApplications.Count,
                ApplicationsClosed = closedApplications.Count
            };
        }

        public async Task<bool> StartFinancialReview(Guid applicationId, string reviewer)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply SET FinancialReviewStatus = @financialReviewStatusInProgress,
                                                UpdatedAt = @updatedAt, UpdatedBy = @updatedBy
                                                WHERE ApplicationId = @applicationId
                                                AND apply.ApplicationStatus = @applicationStatusGatewayAssessed AND apply.DeletedAt IS NULL
                                                AND FinancialReviewStatus IN ( @draftStatus, @newStatus )",
                        new
                        {
                            applicationId,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            financialReviewStatusInProgress = FinancialReviewStatus.InProgress,
                            updatedAt = DateTime.UtcNow,
                            updatedBy = reviewer,
                            draftStatus = FinancialReviewStatus.Draft,
                            newStatus = FinancialReviewStatus.New
                        });
            }

            return true;
        }

        public async Task<bool> RecordFinancialGrade(Guid applicationId, FinancialReviewDetails financialReviewDetails, string financialReviewStatus)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply 
                                                         SET FinancialGrade = @financialReviewDetails,
                                                         FinancialReviewStatus = @financialReviewStatus
                                                         WHERE ApplicationId = @applicationId",
                                                new
                                                {
                                                    applicationId,
                                                    financialReviewDetails,
                                                    financialReviewStatus
                                                });
            }

            return true;
        }

        public async Task<bool> UpdateFinancialReviewDetails(Guid applicationId, FinancialReviewDetails financialReviewDetails)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
                {
                    await connection.ExecuteAsync(@"UPDATE Apply 
                                                         SET FinancialGrade = @financialReviewDetails
                                                         WHERE ApplicationId = @applicationId
                                                            AND apply.DeletedAt IS NULL",
                        new
                        {
                            applicationId,
                            financialReviewDetails
                        });
                }
                return true;
        }

        public async Task<bool> IsUkprnWhitelisted(int ukprn)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleAsync<bool>(@"SELECT
                                                                      CASE WHEN EXISTS 
                                                                      (
                                                                            SELECT UKPRN FROM WhitelistedProviders WHERE UKPRN = @ukprn
                                                                                AND GETUTCDATE() BETWEEN StartDateTime and EndDateTime
                                                                      )
                                                                      THEN 'TRUE'
                                                                      ELSE 'FALSE'
                                                                  END",
                                                                  new { ukprn });
            }
        }

        public async Task UpdateApplicationStatus(Guid applicationId, string status, string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply
                                                SET  ApplicationStatus = @status,
                                                UpdatedBy = @userId,
                                                UpdatedAt = GETUTCDATE()
                                                WHERE ApplicationId = @ApplicationId", new {applicationId, status, userId});
            }
        }

        public async Task<Contact> GetContactForApplication(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleOrDefaultAsync<Contact>(@"SELECT con.* FROM Contacts con 
                                                                        INNER JOIN Apply appl ON appl.CreatedBy = con.Id
                                                                        WHERE appl.ApplicationId = @ApplicationId",
                    new { applicationId });
            }
        }

        public async Task<Organisation> GetOrganisationForApplication(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleOrDefaultAsync<Organisation>(@"SELECT org.* FROM Organisations org 
                                                                        INNER JOIN Apply appl ON appl.OrganisationId = org.Id
                                                                        WHERE appl.ApplicationId = @ApplicationId",
                    new {applicationId});
            }
        }

        public async Task<List<ApplicationOversightDownloadDetails>> GetOversightsForDownload(DateTime dateFrom, DateTime dateTo)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection.QueryAsync<ApplicationOversightDownloadDetails>(@"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
							 org.Name AS OrganisationName,
					        JSON_VALUE(apply.ApplyData, '$.ApplyDetails.UKPRN') AS Ukprn,
                            REPLACE(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName'),' provider','') AS ProviderRoute,
							JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS ApplicationSubmittedDate,
                            REPLACE(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteNameOnRegister'),' provider','') AS ProviderRouteNameOnRegister,
							JSON_VALUE(apply.ApplyData, '$.ApplyDetails.OrganisationType') AS OrganisationType,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.CompaniesHouseDetails.CompanyNumber') AS CompanyNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.Address') AS Address,
                            apply.ApplicationStatus,
							apply.ApplicationDeterminedDate,
                            apply.GatewayReviewStatus as GatewayOutcome,
                            apply.AssessorReviewStatus  as AssessorOutcome,
                            CASE JSON_VALUE(apply.FinancialGrade, '$.SelectedGrade') WHEN @financialGradeInadequate THEN 'Fail' ELSE 'Pass' END as FHCOutcome,
                            CASE WHEN apply.GatewayReviewStatus = @gatewayReviewStatusPass AND apply.AssessorReviewStatus = @assessorReviewStatusApproved AND JSON_VALUE(apply.FinancialGrade, '$.SelectedGrade') <> @financialGradeInadequate THEN 'Pass' ELSE 'Fail' END as OverallOutcome
                            FROM Apply apply
	                        INNER JOIN Organisations org ON org.Id = apply.OrganisationId
                            LEFT JOIN OversightReview r on r.ApplicationId = apply.ApplicationId
	                      WHERE apply.DeletedAt IS NULL
                          AND JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') BETWEEN @dateFrom AND @dateTo
                          and ((GatewayReviewStatus  in (@gatewayReviewStatusPass)
						  and AssessorReviewStatus in (@assessorReviewStatusApproved,@assessorReviewStatusDeclined)
						  and FinancialReviewStatus in (@financialReviewStatusApproved,@financialReviewStatusDeclined, @financialReviewStatusExempt))
                            OR GatewayReviewStatus in (@gatewayReviewStatusFail, @gatewayReviewStatusReject))
						  and r.[Status] is null
                             order by cast(apply.ApplicationDeterminedDate as DATE) ASC, CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ASC,  Org.Name ASC", new
                {
                    gatewayReviewStatusPass = GatewayReviewStatus.Pass,
                    gatewayReviewStatusFail = GatewayReviewStatus.Fail,
                    GatewayReviewStatusReject = GatewayReviewStatus.Rejected,
                    assessorReviewStatusApproved = AssessorReviewStatus.Approved,
                    assessorReviewStatusDeclined = AssessorReviewStatus.Declined,
                    financialReviewStatusApproved = FinancialReviewStatus.Pass,
                    financialReviewStatusDeclined = FinancialReviewStatus.Fail,
                    financialReviewStatusExempt = FinancialReviewStatus.Exempt,
                    oversightReviewStatusPass = OversightReviewStatus.Successful,
                    oversightReviewStatusFail = OversightReviewStatus.Unsuccessful,
                    financialGradeInadequate = Domain.Roatp.FinancialApplicationSelectedGrade.Inadequate,
                    dateFrom = dateFrom.ToString("yyyy-MM-dd"), dateTo = dateTo.AddDays(1).Date.ToString("yyyy-MM-dd")
                })).ToList();
            }
        }

        public async Task<IEnumerable<RoatpApplicationStatus>> GetExistingApplicationStatusByUkprn(string ukprn)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var applicationStatuses = await connection.QueryAsync<RoatpApplicationStatus>(
                    @"select a.Id AS ApplicationId, a.ApplicationStatus AS Status
                      from dbo.Apply a
                      where UKPRN = @ukprn",
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

        public async Task<bool> StartAssessorReview(Guid applicationId, string reviewer)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply SET AssessorReviewStatus = @assessorReviewStatusInProgress,
                                                UpdatedAt = @updatedAt, UpdatedBy = @updatedBy
                                                WHERE ApplicationId = @applicationId
                                                AND AssessorReviewStatus IN ( @draftStatus, @newStatus )",
                        new
                        {
                            applicationId,
                            assessorReviewStatusInProgress = AssessorReviewStatus.InProgress,
                            updatedAt = DateTime.UtcNow,
                            updatedBy = reviewer,
                            draftStatus = AssessorReviewStatus.Draft,
                            newStatus = AssessorReviewStatus.New
                        });
            }

            return await Task.FromResult(true);
        }

        public async Task<ApplyData> GetApplyData(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<ApplyData>(@"SELECT ApplyData FROM Apply WHERE ApplicationId = @applicationId",
                    new { applicationId });
            }
        }

        public async Task<bool> UpdateApplyData(Guid applicationId, ApplyData applyData, string updatedBy)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply SET ApplyData = @applyData,
                                                UpdatedAt = @updatedAt, UpdatedBy = @updatedBy
                                                WHERE ApplicationId = @applicationId",
                            new
                            {
                                applicationId,
                                updatedAt = DateTime.UtcNow,
                                updatedBy,
                                applyData
                            });
            }

            return await Task.FromResult(true);
        }


        private static string GetSortColumnForNew(string requestedColumn)
        {
            switch (requestedColumn)
            {
                case "SubmittedDate":
                    return " CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ";
                case "OutcomeMadeBy":
                    return " JSON_VALUE(apply.FinancialGrade, '$.ClarificationRequestedBy') ";
                case "OutcomeMadeByClosed":
                    return  $@" CASE 
                                WHEN apply.ApplicationStatus = '{ApplicationStatus.Withdrawn}'THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnBy')
                                WHEN apply.ApplicationStatus = '{ApplicationStatus.Removed}' THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedBy')
                                ELSE JSON_VALUE(apply.FinancialGrade, '$.GradedBy')
                            END ";
                case "OutcomeMadeDate":
                    return $@" CASE 
                                WHEN apply.ApplicationStatus = '{ApplicationStatus.Withdrawn}' THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnOn')
                                WHEN apply.ApplicationStatus = '{ApplicationStatus.Removed}' THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedOn')
                                ELSE JSON_VALUE(apply.FinancialGrade, '$.GradedDateTime')
                            END";
                default:
                    return " CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ";
            }
        }

        private static string GetOrderByDirectionFinancial(string sortOrder)
        {
            return "ascending".Equals(sortOrder, StringComparison.InvariantCultureIgnoreCase) ? " ASC " : " DESC ";
        }
    }
}
using Dapper;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.ApplyService.Domain.Apply.Assessor;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Data
{
    public class AssessorRepository : IAssessorRepository
    {
        private readonly IApplyConfig _config;
        private readonly ILogger<AssessorRepository> _logger;

        private const string ApplicationSummaryFields = @"ApplicationId,
                            org.Name AS OrganisationName,
                            ApplicationStatus,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.UKPRN') AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ProviderRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate,
                            Assessor1Name,
                            Assessor2Name,
                            Assessor1UserId,
                            Assessor2UserId
            ";

        private const string NewApplicationsWhereClause = @"
                            apply.DeletedAt IS NULL AND apply.GatewayReviewStatus = @gatewayReviewStatusApproved
                            AND apply.ApplicationStatus = @applicationStatusGatewayAssessed
                            -- Not assigned to current user
                            AND ISNULL(apply.Assessor1UserId, '') <> @userId AND ISNULL(apply.Assessor2UserId, '') <> @userId
                            -- Can be assigned to at least one assessor
                            AND (apply.Assessor1UserId IS NULL OR apply.Assessor2UserId IS NULL)
                            ";

        private const string InProgressApplicationsWhereClause = @"
                            apply.DeletedAt IS NULL AND apply.GatewayReviewStatus = @gatewayReviewStatusApproved
                            AND apply.ApplicationStatus = @applicationStatusGatewayAssessed
                            AND
                            (
                                -- Current user is Assessor 1 and in progress (or hasn't been picked up by Assessor 2)
                                (apply.Assessor1UserId = @userId AND (apply.Assessor1ReviewStatus = @inProgressReviewStatus OR apply.Assessor2UserId IS NULL))
                                OR 
                                -- Current user is Assessor 2 and in progress (or hasn't been picked up by Assessor 1)
                                (apply.Assessor2UserId = @userId AND (apply.Assessor2ReviewStatus = @inProgressReviewStatus OR apply.Assessor1UserId IS NULL))
                                OR
                                -- Both Assessors assigned but at least one is still in progress
                                (apply.Assessor1UserId IS NOT NULL AND apply.Assessor2UserId IS NOT NULL AND (apply.Assessor1ReviewStatus = @inProgressReviewStatus OR Assessor2ReviewStatus = @inProgressReviewStatus))
                            )";

        private const string InModerationApplicationsWhereClause = @"
                            apply.DeletedAt IS NULL AND apply.GatewayReviewStatus = @gatewayReviewStatusApproved
                            AND apply.ApplicationStatus = @applicationStatusGatewayAssessed
                            AND Assessor1ReviewStatus = @approvedReviewStatus AND Assessor2ReviewStatus = @approvedReviewStatus
                            AND ModerationStatus IN (@newModerationStatus, @inProgressModerationStatus)";

        private const string InClarificationApplicationsWhereClause = @"
                            apply.DeletedAt IS NULL AND apply.GatewayReviewStatus = @gatewayReviewStatusApproved
                            AND apply.ApplicationStatus = @applicationStatusGatewayAssessed
                            AND Assessor1ReviewStatus = @approvedReviewStatus AND Assessor2ReviewStatus = @approvedReviewStatus
                            AND ModerationStatus = @clarificationSentModerationStatus";

        private const string ClosedApplicationsWhereClause = @"
                            apply.DeletedAt IS NULL
                            AND ( apply.ApplicationStatus IN (@applicationStatusWithdrawn, @applicationStatusRemoved)
                                  OR (
                                      Assessor1ReviewStatus = @approvedReviewStatus AND Assessor2ReviewStatus = @approvedReviewStatus
                                      AND ModerationStatus IN (@passModerationStatus, @failModerationStatus)
                                     )
                                )";

        public AssessorRepository(IConfigurationService configurationService, ILogger<AssessorRepository> logger)
        {
            _logger = logger;
            _config = configurationService.GetConfig().GetAwaiter().GetResult();
        }

        public async Task<List<AssessorApplicationSummary>> GetNewAssessorApplications(string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<AssessorApplicationSummary>(
                        $@"SELECT 
                            {ApplicationSummaryFields}
	                       FROM Apply apply
	                       INNER JOIN Organisations org ON org.Id = apply.OrganisationId
	                       WHERE {NewApplicationsWhereClause}
                           ORDER BY CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ASC, org.Name ASC",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            userId = userId
                        })).ToList();
            }
        }

        public async Task<int> GetNewAssessorApplicationsCount(string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .ExecuteScalarAsync<int>(
                        $@"SELECT COUNT(1)
	                      FROM Apply apply
	                      WHERE {NewApplicationsWhereClause}",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            userId = userId
                        }));
            }
        }

        public async Task AssignAssessor1(Guid applicationId, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply
                                                SET Assessor1UserId = @userId,
                                                    Assessor1Name = @userName,
                                                    Assessor1ReviewStatus = @inProgressReviewStatus
                                                WHERE ApplicationId = @applicationId",
                    new
                    {
                        applicationId,
                        userId,
                        userName,
                        inProgressReviewStatus = AssessorReviewStatus.InProgress
                    });

                // must clear down all existing page reviews as the assessor has changed
                await connection.ExecuteAsync(@"UPDATE AssessorPageReviewOutcome
                                                SET Assessor1UserId = @userId,
                                                    Assessor1ReviewStatus = NULL,
                                                    Assessor1ReviewComment = NULL,
                                                    UpdatedAt = @updatedAt,
                                                    UpdatedBy = @userId
                                                WHERE ApplicationId = @applicationId",
                    new
                    {
                        applicationId,
                        userId,
                        updatedAt = DateTime.UtcNow
                    });
            }
        }

        public async Task AssignAssessor2(Guid applicationId, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.ExecuteAsync(@"UPDATE Apply
                                                SET Assessor2UserId = @userId,
                                                    Assessor2Name = @userName,
                                                    Assessor2ReviewStatus = @inProgressReviewStatus
                                                WHERE ApplicationId = @applicationId",
                    new
                    {
                        applicationId,
                        userId,
                        userName,
                        inProgressReviewStatus = AssessorReviewStatus.InProgress
                    });

                // must clear down all existing page reviews as the assessor has changed
                await connection.ExecuteAsync(@"UPDATE AssessorPageReviewOutcome
                                                SET Assessor2UserId = @userId,
                                                    Assessor2ReviewStatus = NULL,
                                                    Assessor2ReviewComment = NULL,
                                                    UpdatedAt = @updatedAt,
                                                    UpdatedBy = @userId
                                                WHERE ApplicationId = @applicationId",
                    new
                    {
                        applicationId,
                        userId,
                        updatedAt = DateTime.UtcNow
                    });
            }
        }

        public async Task<List<AssessorApplicationSummary>> GetInProgressAssessorApplications(string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<AssessorApplicationSummary>(
                        $@"SELECT 
                            {ApplicationSummaryFields}
	                        FROM Apply apply
	                        INNER JOIN Organisations org ON org.Id = apply.OrganisationId
	                        WHERE {InProgressApplicationsWhereClause}
                            ORDER BY CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ASC, org.Name ASC",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            inProgressReviewStatus = AssessorReviewStatus.InProgress,
                            userId = userId
                        })).ToList();
            }
        }

        public async Task<int> GetInProgressAssessorApplicationsCount(string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .ExecuteScalarAsync<int>(
                        $@"SELECT COUNT(1)
	                      FROM Apply apply
	                      WHERE {InProgressApplicationsWhereClause}",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            inProgressReviewStatus = AssessorReviewStatus.InProgress,
                            userId = userId
                        }));
            }
        }

        public async Task<List<ModerationApplicationSummary>> GetApplicationsInModeration()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<ModerationApplicationSummary>(
                        $@"SELECT 
                            {ApplicationSummaryFields}
                            , ModerationStatus
                            , JSON_VALUE(apply.ApplyData, '$.ModeratorReviewDetails.ModeratorName') AS ModeratorName
	                        FROM Apply apply
	                        INNER JOIN Organisations org ON org.Id = apply.OrganisationId
                            WHERE {InModerationApplicationsWhereClause}
	                        ORDER BY CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ASC, org.Name ASC",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            approvedReviewStatus = AssessorReviewStatus.Approved,
                            newModerationStatus = ModerationStatus.New,
                            inProgressModerationStatus = ModerationStatus.InProgress
                        })).ToList();
            }
        }

        public async Task<int> GetApplicationsInModerationCount()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .ExecuteScalarAsync<int>(
                        $@"SELECT COUNT(1)
	                      FROM Apply apply
	                      WHERE {InModerationApplicationsWhereClause}",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            approvedReviewStatus = AssessorReviewStatus.Approved,
                            newModerationStatus = ModerationStatus.New,
                            inProgressModerationStatus = ModerationStatus.InProgress
                        }));
            }
        }

        public async Task<List<ClarificationApplicationSummary>> GetApplicationsInClarification()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<ClarificationApplicationSummary>(
                        $@"SELECT 
                            {ApplicationSummaryFields}
                            , JSON_VALUE(apply.ApplyData, '$.ModeratorReviewDetails.ModeratorName') AS ModeratorName
                            , JSON_VALUE(apply.ApplyData, '$.ModeratorReviewDetails.ClarificationRequestedOn') AS ClarificationRequestedOn
	                        FROM Apply apply
	                        INNER JOIN Organisations org ON org.Id = apply.OrganisationId
	                        WHERE {InClarificationApplicationsWhereClause}
                            ORDER BY CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ASC, org.Name ASC",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            approvedReviewStatus = AssessorReviewStatus.Approved,
                            newModerationStatus = ModerationStatus.New,
                            clarificationSentModerationStatus = ModerationStatus.ClarificationSent
                        })).ToList();
            }
        }

        public async Task<int> GetApplicationsInClarificationCount()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .ExecuteScalarAsync<int>(
                        $@"SELECT COUNT(1)
	                      FROM Apply apply
	                      WHERE {InClarificationApplicationsWhereClause}",
                        new
                        {
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            applicationStatusGatewayAssessed = ApplicationStatus.GatewayAssessed,
                            approvedReviewStatus = AssessorReviewStatus.Approved,
                            newModerationStatus = ModerationStatus.New,
                            clarificationSentModerationStatus = ModerationStatus.ClarificationSent
                        }));
            }
        }

        public async Task<List<ClosedApplicationSummary>> GetClosedApplications()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .QueryAsync<ClosedApplicationSummary>(
                        $@"SELECT 
                            {ApplicationSummaryFields}
                            , ModerationStatus
                            , CASE
                                WHEN apply.ApplicationStatus = @applicationStatusWithdrawn THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnOn')
                                WHEN apply.ApplicationStatus = @applicationStatusRemoved THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedOn')
                                ELSE JSON_VALUE(apply.ApplyData, '$.ModeratorReviewDetails.OutcomeDateTime')
                              END AS OutcomeMadeDate
                            , CASE
                                WHEN apply.ApplicationStatus = @applicationStatusWithdrawn THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnBy')
                                WHEN apply.ApplicationStatus = @applicationStatusRemoved THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedBy')
                                ELSE JSON_VALUE(apply.ApplyData, '$.ModeratorReviewDetails.ModeratorName')
                              END AS OutcomeMadeBy                            
	                        FROM Apply apply
	                        INNER JOIN Organisations org ON org.Id = apply.OrganisationId
	                        WHERE {ClosedApplicationsWhereClause}
                            ORDER BY CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE) ASC, org.Name ASC",
                        new
                        {
                            applicationStatusWithdrawn = ApplicationStatus.Withdrawn,
                            applicationStatusRemoved = ApplicationStatus.Removed,
                            approvedReviewStatus = AssessorReviewStatus.Approved,
                            passModerationStatus = ModerationStatus.Pass,
                            failModerationStatus = ModerationStatus.Fail
                        })).ToList();
            }
        }

        public async Task<int> GetClosedApplicationsCount()
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection
                    .ExecuteScalarAsync<int>(
                        $@"SELECT COUNT(1)
	                      FROM Apply apply
	                      WHERE {ClosedApplicationsWhereClause}",
                        new
                        {
                            applicationStatusWithdrawn = ApplicationStatus.Withdrawn,
                            applicationStatusRemoved = ApplicationStatus.Removed,
                            approvedReviewStatus = AssessorReviewStatus.Approved,
                            passModerationStatus = ModerationStatus.Pass,
                            failModerationStatus = ModerationStatus.Fail
                        }));
            }
        }

        private async Task<int> GetAssessorNumber(Guid applicationId, string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.ExecuteScalarAsync<int>(
                        $@"SELECT
                                CASE WHEN (Assessor1UserId = @userId) THEN 1
                                     WHEN (Assessor2UserId = @userId) THEN 2
                                     ELSE 0
                                END
                           FROM [Apply]
                           WHERE [ApplicationId] = @applicationId",
                        new
                        {
                            userId = userId,
                            applicationId = applicationId
                        });
            }
        }

        public async Task SubmitAssessorPageOutcome(Guid applicationId, int sequenceNumber, int sectionNumber, string pageId,
                                                    string userId, string userName, string status, string comment)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var assessorNumber = await GetAssessorNumber(applicationId, userId);

                // NOTE: CreateEmptyAssessorReview should have been called before getting to this point.
                // This is so that all PageReviewOutcomes are initialized for the Assessor
                await connection.ExecuteAsync(
                    @"IF (@assessorNumber = 1)
		                BEGIN
			                UPDATE [AssessorPageReviewOutcome]
			                   SET [Assessor1UserId] = @userId
                                   , [Assessor1ReviewStatus] = @status
				                   , [Assessor1ReviewComment] = @comment
				                   , [UpdatedAt] = GETUTCDATE()
				                   , [UpdatedBy] = @userId
			                WHERE [ApplicationId] = @applicationId AND
					              [SequenceNumber] = @sequenceNumber AND
					              [SectionNumber] = @sectionNumber AND
					              [PageId] = @pageId
		                END                                                         
                      IF (@assessorNumber = 2)
                        BEGIN
			                UPDATE [AssessorPageReviewOutcome]
			                   SET [Assessor2UserId] = @userId
                                   , [Assessor2ReviewStatus] = @status
				                   , [Assessor2ReviewComment] = @comment
				                   , [UpdatedAt] = GETUTCDATE()
				                   , [UpdatedBy] = @userId
			                WHERE [ApplicationId] = @applicationId AND
					              [SequenceNumber] = @sequenceNumber AND
					              [SectionNumber] = @sectionNumber AND
					              [PageId] = @pageId                 
                        END",
                    new { applicationId, sequenceNumber, sectionNumber, pageId, assessorNumber, userId, status, comment });
            }
        }

        public async Task<AssessorPageReviewOutcome> GetAssessorPageReviewOutcome(Guid applicationId,
                                                                    int sequenceNumber,
                                                                    int sectionNumber,
                                                                    string pageId,
                                                                    string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var assessorNumber = await GetAssessorNumber(applicationId, userId);

                var pageReviewOutcomeResults = await connection.QueryAsync<AssessorPageReviewOutcome>(
                                                                @"IF (@assessorNumber = 1)
	                                                                BEGIN
		                                                                SELECT [ApplicationId]
			                                                                  ,[SequenceNumber]
			                                                                  ,[SectionNumber]
			                                                                  ,[PageId]
			                                                                  ,@assessorNumber AS AssessorNumber
			                                                                  ,[Assessor1UserId] AS UserId
			                                                                  ,[Assessor1ReviewStatus] AS [Status]
			                                                                  ,[Assessor1ReviewComment] AS Comment
		                                                                  FROM [dbo].[AssessorPageReviewOutcome]
		                                                                  WHERE [ApplicationId] = @applicationId AND
				                                                                [SequenceNumber] = @sequenceNumber AND
				                                                                [SectionNumber] = @sectionNumber AND
				                                                                [PageId] = @pageId AND
				                                                                [Assessor1UserId] = @userId                                                        
	                                                                END
                                                                IF (@assessorNumber = 2)
	                                                                BEGIN
		                                                                SELECT [ApplicationId]
			                                                                  ,[SequenceNumber]
			                                                                  ,[SectionNumber]
			                                                                  ,[PageId]
			                                                                  ,@assessorNumber AS AssessorNumber
			                                                                  ,[Assessor2UserId] AS UserId
			                                                                  ,[Assessor2ReviewStatus] AS [Status]
			                                                                  ,[Assessor2ReviewComment] AS Comment
		                                                                  FROM [dbo].[AssessorPageReviewOutcome]
		                                                                  WHERE [ApplicationId] = @applicationId AND
				                                                                [SequenceNumber] = @sequenceNumber AND
				                                                                [SectionNumber] = @sectionNumber AND
				                                                                [PageId] = @pageId AND
				                                                                [Assessor2UserId] = @userId                      
	                                                                END",
                    new { applicationId, sequenceNumber, sectionNumber, pageId, assessorNumber, userId });

                return pageReviewOutcomeResults.FirstOrDefault();
            }
        }

        public async Task<List<AssessorPageReviewOutcome>> GetAssessorPageReviewOutcomesForSection(Guid applicationId,
                                                            int sequenceNumber,
                                                            int sectionNumber,
                                                            string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var assessorNumber = await GetAssessorNumber(applicationId, userId);

                var pageReviewOutcomeResults = await connection.QueryAsync<AssessorPageReviewOutcome>(
                                                                @"IF (@assessorNumber = 1)
	                                                                BEGIN
		                                                                SELECT [ApplicationId]
			                                                                  ,[SequenceNumber]
			                                                                  ,[SectionNumber]
			                                                                  ,[PageId]
			                                                                  ,@assessorNumber AS AssessorNumber
			                                                                  ,[Assessor1UserId] AS UserId
			                                                                  ,[Assessor1ReviewStatus] AS [Status]
			                                                                  ,[Assessor1ReviewComment] AS Comment
		                                                                  FROM [dbo].[AssessorPageReviewOutcome]
		                                                                  WHERE [ApplicationId] = @applicationId AND
				                                                                [SequenceNumber] = @sequenceNumber AND
				                                                                [SectionNumber] = @sectionNumber AND
				                                                                [Assessor1UserId] = @userId                                                        
	                                                                END
                                                                IF (@assessorNumber = 2)
	                                                                BEGIN
		                                                                SELECT [ApplicationId]
			                                                                  ,[SequenceNumber]
			                                                                  ,[SectionNumber]
			                                                                  ,[PageId]
			                                                                  ,@assessorNumber AS AssessorNumber
			                                                                  ,[Assessor2UserId] AS UserId
			                                                                  ,[Assessor2ReviewStatus] AS [Status]
			                                                                  ,[Assessor2ReviewComment] AS Comment
		                                                                  FROM [dbo].[AssessorPageReviewOutcome]
		                                                                  WHERE [ApplicationId] = @applicationId AND
				                                                                [SequenceNumber] = @sequenceNumber AND
				                                                                [SectionNumber] = @sectionNumber AND
				                                                                [Assessor2UserId] = @userId                      
	                                                                END",
                    new { applicationId, sequenceNumber, sectionNumber, assessorNumber, userId });

                return pageReviewOutcomeResults.ToList();
            }
        }

        public async Task<List<AssessorPageReviewOutcome>> GetAllAssessorPageReviewOutcomes(Guid applicationId, string userId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var assessorNumber = await GetAssessorNumber(applicationId, userId);

                var pageReviewOutcomeResults = await connection.QueryAsync<AssessorPageReviewOutcome>(
                                                                @"IF (@assessorNumber = 1)
	                                                                BEGIN
		                                                                SELECT [ApplicationId]
			                                                                  ,[SequenceNumber]
			                                                                  ,[SectionNumber]
			                                                                  ,[PageId]
			                                                                  ,@assessorNumber AS AssessorNumber
			                                                                  ,[Assessor1UserId] AS UserId
			                                                                  ,[Assessor1ReviewStatus] AS [Status]
			                                                                  ,[Assessor1ReviewComment] AS Comment
		                                                                  FROM [dbo].[AssessorPageReviewOutcome]
		                                                                  WHERE [ApplicationId] = @applicationId AND
				                                                                [Assessor1UserId] = @userId                                                        
	                                                                END
                                                                IF (@assessorNumber = 2)
	                                                                BEGIN
		                                                                SELECT [ApplicationId]
			                                                                  ,[SequenceNumber]
			                                                                  ,[SectionNumber]
			                                                                  ,[PageId]
			                                                                  ,@assessorNumber AS AssessorNumber
			                                                                  ,[Assessor2UserId] AS UserId
			                                                                  ,[Assessor2ReviewStatus] AS [Status]
			                                                                  ,[Assessor2ReviewComment] AS Comment
		                                                                  FROM [dbo].[AssessorPageReviewOutcome]
		                                                                  WHERE [ApplicationId] = @applicationId AND
				                                                                [Assessor2UserId] = @userId                      
	                                                                END",
                    new { applicationId, assessorNumber, userId });

                return pageReviewOutcomeResults.ToList();
            }
        }

		public async Task UpdateAssessorReviewStatus(Guid applicationId, string userId, string status)
		{
			using (var connection = new SqlConnection(_config.SqlConnectionString))
			{
                var assessorNumber = await GetAssessorNumber(applicationId, userId);

                await connection.ExecuteAsync(
                    @"IF (@assessorNumber = 1)
                        BEGIN
		                    UPDATE [Apply]
			                        SET Assessor1ReviewStatus = @status
                                        , UpdatedAt = GETUTCDATE()
				                        , UpdatedBy = @userId
			                        WHERE ApplicationId = @applicationId AND DeletedAt IS NULL AND Assessor1UserId = @userId
                        END
                      IF (@assessorNumber = 2)
                        BEGIN
		                    UPDATE [Apply]
			                        SET Assessor2ReviewStatus = @status
                                        , UpdatedAt = GETUTCDATE()
				                        , UpdatedBy = @userId
			                        WHERE ApplicationId = @applicationId AND DeletedAt IS NULL AND Assessor2UserId = @userId                
                        END",
					new { applicationId, assessorNumber, userId, status });
            }
		}

        public async Task CreateEmptyAssessorReview(Guid applicationId, string userId, string userName, List<AssessorPageReviewOutcome> pageReviewOutcomes)
        {
            var assessorNumber = await GetAssessorNumber(applicationId, userId);
            var createdAtDateTime = DateTime.UtcNow;

            var dataTable = new DataTable();
            dataTable.Columns.Add("ApplicationId", typeof(Guid));
            dataTable.Columns.Add("SequenceNumber", typeof(int));
            dataTable.Columns.Add("SectionNumber", typeof(int));
            dataTable.Columns.Add("PageId", typeof(string));
            dataTable.Columns.Add("Assessor1UserId", typeof(string));
            dataTable.Columns.Add("Assessor2UserId", typeof(string));
            dataTable.Columns.Add("CreatedAt", typeof(DateTime));
            dataTable.Columns.Add("CreatedBy", typeof(string));

            foreach (var outcome in pageReviewOutcomes)
            {
                dataTable.Rows.Add(
                    applicationId,
                    outcome.SequenceNumber,
                    outcome.SectionNumber,
                    outcome.PageId,
                    assessorNumber == 1 ? userId : null,
                    assessorNumber == 2 ? userId : null,
                    createdAtDateTime,
                    userId
                );
            }

            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                await connection.OpenAsync();
                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, null))
                {
                    bulkCopy.DestinationTableName = "AssessorPageReviewOutcome";
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                }
                connection.Close();
            }
        }
    }
}
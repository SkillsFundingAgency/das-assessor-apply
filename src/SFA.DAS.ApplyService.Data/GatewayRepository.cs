using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using SFA.DAS.ApplyService.Configuration;
using SFA.DAS.ApplyService.Data.DapperTypeHandlers;
using SFA.DAS.ApplyService.Domain.Apply;
using SFA.DAS.ApplyService.Domain.Apply.Gateway;
using SFA.DAS.ApplyService.Domain.Entities;
using SFA.DAS.ApplyService.Domain.Interfaces;

namespace SFA.DAS.ApplyService.Data
{
    public class GatewayRepository : IGatewayRepository
    {
        private readonly IApplyConfig _config;
        private readonly ILogger<GatewayRepository> _logger;

        public GatewayRepository(IConfigurationService configurationService, ILogger<GatewayRepository> logger)
        {
            _logger = logger;
            _config = configurationService.GetConfig().GetAwaiter().GetResult();

            SqlMapper.AddTypeHandler(typeof(ApplyData), new ApplyDataHandler());
        }

        private async Task<ApplyData> GetApplyData(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<ApplyData>(@"SELECT ApplyData FROM Apply WHERE ApplicationId = @applicationId",
                    new { applicationId });
            }
        }

        public async Task<bool> WithdrawApplication(Guid applicationId, string comments, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var applyData = await GetApplyData(applicationId);

                if (applyData?.ApplyDetails != null)
                {
                    applyData.ApplyDetails.ApplicationWithdrawnOn = DateTime.UtcNow;
                    applyData.ApplyDetails.ApplicationWithdrawnBy = userName;
                }

                var rowsAffected = await connection.ExecuteAsync(@"UPDATE app
                                                SET  app.ApplicationStatus = @applicationStatusWithdrawn,
                                                     app.Comments = @comments,
                                                     app.ApplyData = @applyData,
                                                     app.UpdatedAt = GETUTCDATE(),
                                                     app.UpdatedBy = @updatedBy
                                                FROM Apply app
                                                LEFT JOIN OversightReview outcome on outcome.ApplicationId = app.ApplicationId
                                                WHERE app.ApplicationId = @applicationId
                                                AND outcome.[Status] IS NULL",
                                                new
                                                {
                                                    applicationId,
                                                    comments,
                                                    applyData,
                                                    updatedBy = userName,
                                                    applicationStatusWithdrawn = ApplicationStatus.Withdrawn
                                                });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> RemoveApplication(Guid applicationId, string comments, string externalComments, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var applyData = await GetApplyData(applicationId);

                if (applyData?.ApplyDetails != null)
                {
                    applyData.ApplyDetails.ApplicationRemovedOn = DateTime.UtcNow;
                    applyData.ApplyDetails.ApplicationRemovedBy = userName;
                }

                var rowsAffected = await connection.ExecuteAsync(@"UPDATE Apply
                                                SET  ApplicationStatus = @applicationStatusRemoved,
                                                     Comments = @comments,
                                                     ExternalComments = @externalComments,
                                                     ApplyData = @applyData,
                                                     UpdatedAt = GETUTCDATE(),
                                                     UpdatedBy = @updatedBy
                                                WHERE ApplicationId = @applicationId",
                                                new
                                                {
                                                    applicationId,
                                                    comments,
                                                    externalComments,
                                                    applyData,
                                                    updatedBy = userName,
                                                    applicationStatusRemoved = ApplicationStatus.Removed
                                                });

                return rowsAffected > 0;
            }
        }

            public async Task<List<RoatpGatewaySummaryItem>> GetNewGatewayApplications(string searchTerm, string sortOrder)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var orderByClause = $"{GetSortColumnForNew()} { GetOrderByDirection(sortOrder)}";

                return (await connection
                    .QueryAsync<RoatpGatewaySummaryItem>(
                        $@"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
                            apply.ApplicationStatus AS ApplicationStatus,
                            apply.GatewayReviewStatus AS GatewayReviewStatus,
                            apply.AssessorReviewStatus AS AssessorReviewStatus,
                            apply.FinancialReviewStatus AS FinancialReviewStatus,
                            org.Name AS OrganisationName,
                            apply.UKPRN AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ApplicationRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate
	                      FROM Apply apply
	                      INNER JOIN Organisations org ON org.Id = apply.OrganisationId
	                      WHERE apply.ApplicationStatus = @applicationStatusSubmitted AND apply.DeletedAt IS NULL
	                        AND apply.GatewayReviewStatus = @gatewayReviewStatusNew
                            AND ( 
                                    @searchString = '%%'
                                    OR apply.UKPRN LIKE @searchString
                                    OR org.Name LIKE  @searchString
                                )
                          ORDER BY {orderByClause}, org.Name ASC",
                        new
                        {
                            applicationStatusSubmitted = ApplicationStatus.Submitted,
                            gatewayReviewStatusNew = GatewayReviewStatus.New,
                            searchString = $"%{searchTerm}%"
                        })).ToList();
            }
        }

        public async Task<List<RoatpGatewaySummaryItem>> GetInProgressGatewayApplications(string searchTerm, string sortColumn, string sortOrder)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var orderByClause = $"{GetSortColumnForInProgress(sortColumn)} { GetOrderByDirection(sortOrder)}";

                return (await connection
                    .QueryAsync<RoatpGatewaySummaryItem>(
                        $@"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
                            apply.ApplicationStatus AS ApplicationStatus,
                            apply.GatewayReviewStatus AS GatewayReviewStatus,
                            apply.AssessorReviewStatus AS AssessorReviewStatus,
                            apply.FinancialReviewStatus AS FinancialReviewStatus,
                            apply.GatewayUserName AS LastCheckedBy,
                            org.Name AS OrganisationName,
                            apply.UKPRN AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ApplicationRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate,
                            JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.ClarificationRequestedOn') AS ClarificationRequestedDate
	                      FROM Apply apply
	                      INNER JOIN Organisations org ON org.Id = apply.OrganisationId
	                      WHERE apply.ApplicationStatus = @applicationStatusSubmitted AND apply.DeletedAt IS NULL
	                        AND apply.GatewayReviewStatus in (@gatewayReviewStatusInProgress, @gatewayReviewStatusClarificationSent)
                            AND ( 
                                    @searchString = '%%'
                                    OR apply.UKPRN LIKE @searchString
                                    OR org.Name LIKE  @searchString
                                )
                          ORDER BY {orderByClause}, org.Name ASC",
                        new
                        {
                            applicationStatusSubmitted = ApplicationStatus.Submitted,
                            gatewayReviewStatusInProgress = GatewayReviewStatus.InProgress,
                            gatewayReviewStatusClarificationSent = GatewayReviewStatus.ClarificationSent,
                            searchString = $"%{searchTerm}%"
                        })).ToList();
            }
        }

        public async Task<List<RoatpGatewaySummaryItem>> GetClosedGatewayApplications(string searchTerm, string sortColumn, string sortOrder)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var orderByClause = $"{GetSortColumnForOutcome(sortColumn)} { GetOrderByDirection(sortOrder)}";

                return (await connection
                    .QueryAsync<RoatpGatewaySummaryItem>(
                        $@"SELECT 
                            apply.Id AS Id,
                            apply.ApplicationId AS ApplicationId,
                            apply.ApplicationStatus AS ApplicationStatus,
                            apply.GatewayReviewStatus AS GatewayReviewStatus,
                            apply.AssessorReviewStatus AS AssessorReviewStatus,
                            apply.FinancialReviewStatus AS FinancialReviewStatus,
                            apply.GatewayUserName AS LastCheckedBy,                            
                            org.Name AS OrganisationName,
                            apply.UKPRN AS Ukprn,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ReferenceNumber') AS ApplicationReferenceNumber,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ProviderRouteName') AS ApplicationRoute,
                            JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS SubmittedDate,
                            CASE 
                                WHEN apply.ApplicationStatus = @applicationStatusWithdrawn THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnOn')
                                WHEN apply.ApplicationStatus = @applicationStatusRemoved THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedOn')
                                ELSE JSON_VALUE(apply.ApplyData, '$.GatewayReviewDetails.OutcomeDateTime')
                            END AS OutcomeMadeDate,
                            CASE 
                                WHEN apply.ApplicationStatus = @applicationStatusWithdrawn THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnBy')
                                WHEN apply.ApplicationStatus = @applicationStatusRemoved THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedBy')
                                ELSE apply.GatewayUserName
                            END AS OutcomeMadeBy
	                      FROM Apply apply
	                      INNER JOIN Organisations org ON org.Id = apply.OrganisationId
	                      WHERE apply.DeletedAt IS NULL
                            AND (
                                    apply.ApplicationStatus in (@applicationStatusWithdrawn, @applicationStatusRemoved)
                                    OR apply.GatewayReviewStatus IN (@gatewayReviewStatusApproved, @gatewayReviewStatusFailed, @gatewayReviewStatusRejected)
                                )
                            AND ( 
                                    @searchString = '%%'
                                    OR apply.UKPRN LIKE @searchString
                                    OR org.Name LIKE  @searchString
                                )
                          ORDER BY {orderByClause}, org.Name ASC",
                        new
                        {
                            applicationStatusWithdrawn = ApplicationStatus.Withdrawn,
                            applicationStatusRemoved = ApplicationStatus.Removed,
                            gatewayReviewStatusApproved = GatewayReviewStatus.Pass,
                            gatewayReviewStatusFailed = GatewayReviewStatus.Fail,
                            gatewayReviewStatusRejected = GatewayReviewStatus.Rejected,
                            searchString = $"%{searchTerm}%"
                        })).ToList();
            }
        }

        public async Task<IEnumerable<GatewayApplicationStatusCount>> GetGatewayApplicationStatusCounts(string searchTerm)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var applicationStatuses = await connection.QueryAsync<GatewayApplicationStatusCount>(
                    $@"SELECT
                        apply.GatewayReviewStatus,
                        apply.ApplicationStatus,
                        COUNT(1) as 'Count'
                        FROM Apply apply
	                    INNER JOIN Organisations org ON org.Id = apply.OrganisationId
                        WHERE apply.DeletedAt IS NULL
                            AND ( 
                                    @searchString = '%%'
                                    OR apply.UKPRN LIKE @searchString
                                    OR org.Name LIKE  @searchString
                                )
                        GROUP BY GatewayReviewStatus, ApplicationStatus
                        ", 
                    new 
                    { 
                        searchString = $"%{searchTerm}%" 
                    });

                return applicationStatuses;
            }
        }

        public async Task<bool> StartGatewayReview(Guid applicationId, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var rowsAffected = await connection.ExecuteAsync(@"UPDATE Apply
                                                SET  GatewayReviewStatus = @gatewayReviewStatusInProgress,
                                                     UpdatedBy = @userName, UpdatedAt = GETUTCDATE(),
                                                     GatewayUserId = @userId, GatewayUserName = @userName
                                                WHERE ApplicationId = @applicationId AND GatewayReviewStatus = @gatewayReviewStatusNew",
                                                new
                                                {
                                                    applicationId,
                                                    userId,
                                                    userName,
                                                    gatewayReviewStatusNew = GatewayReviewStatus.New,
                                                    gatewayReviewStatusInProgress = GatewayReviewStatus.InProgress,
                                                });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> EvaluateGateway(Guid applicationId, bool isGatewayApproved, string userId, string userName)
        {
            var applicationStatus = isGatewayApproved ? ApplicationStatus.GatewayAssessed : ApplicationStatus.Rejected;   
            var gatewayReviewStatus = isGatewayApproved ? GatewayReviewStatus.Pass : GatewayReviewStatus.Fail;

            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var rowsAffected = await connection.ExecuteAsync(@"UPDATE Apply
                                                    SET  ApplicationStatus = @applicationStatus, GatewayReviewStatus = @gatewayReviewStatus,
                                                         UpdatedBy = @userName, UpdatedAt = GETUTCDATE(),
                                                         GatewayUserId = @userId, GatewayUserName = @userName
                                                    WHERE ApplicationId = @applicationId AND GatewayReviewStatus = @gatewayReviewStatusInProgress",
                                                new
                                                {
                                                    applicationId,
                                                    applicationStatus,
                                                    gatewayReviewStatus,
                                                    userId,
                                                    userName,
                                                    gatewayReviewStatusInProgress = GatewayReviewStatus.InProgress
                                                });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateGatewayApplyData(Guid applicationId, ApplyData applyData, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var rowsAffected = await connection.ExecuteAsync(@"UPDATE [Apply]
                                                   SET [ApplyData] = @applyData
                                                      ,[UpdatedAt] = GetUTCDATE()
                                                      ,[UpdatedBy] = @userName 
                                                      ,[GatewayUserId] = @userId
                                                      ,[GatewayUsername] = @userName
                                                 WHERE [ApplicationId] = @applicationId",
                    new { applicationId, applyData, userId, userName });

                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateGatewayReviewStatusAndComment(Guid applicationId, ApplyData applyData, string gatewayReviewStatus, string userId, string userName)
        {
            var applicationStatus = ApplicationStatus.GatewayAssessed;

            if (gatewayReviewStatus == GatewayReviewStatus.ClarificationSent)
            {
                applicationStatus = ApplicationStatus.Submitted;
            }

            if (gatewayReviewStatus == GatewayReviewStatus.Rejected)
            {
                applicationStatus = ApplicationStatus.Rejected;
            }


            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var rowsAffected = await connection.ExecuteAsync(@"UPDATE [Apply]
                                                   SET [ApplicationStatus] = @applicationStatus
                                                      ,[GatewayReviewStatus] = @gatewayReviewStatus
                                                      ,[ApplyData] = @applyData
                                                      ,[UpdatedAt] = GetUTCDATE()
                                                      ,[UpdatedBy] = @userName 
                                                      ,[GatewayUserId] = @userId
                                                      ,[GatewayUsername] = @userName
                                                 WHERE [ApplicationId] = @applicationId",
                    new { applicationId, applicationStatus, gatewayReviewStatus, applyData, userId, userName });

                return rowsAffected > 0;
            }
        }

        public async Task<List<GatewayPageAnswerSummary>> GetGatewayPageAnswers(Guid applicationId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return (await connection.QueryAsync<GatewayPageAnswerSummary>(@"SELECT ApplicationId, PageId, Status, Comments FROM GatewayAnswer
                                                    WHERE ApplicationId = @applicationId",
                                                    new { applicationId })).ToList();
            }
        }

        public async Task<GatewayPageAnswer> GetGatewayPageAnswer(Guid applicationId, string pageId)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                return await connection.QuerySingleOrDefaultAsync<GatewayPageAnswer>(@"SELECT * from GatewayAnswer
                                                    WHERE applicationId = @applicationId and pageid = @pageId",
                                                    new { applicationId, pageId });
            }
        }

        public async Task<bool> InsertGatewayPageAnswer(GatewayPageAnswer pageAnswer, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var rowsAffected = await connection.ExecuteAsync(
                                    @"INSERT INTO GatewayAnswer ([Id], [ApplicationId], [PageId], [Status], [comments], [UpdatedAt], [UpdatedBy])
														 VALUES (@id, @applicationId, @pageId, @status, @comments, @updatedAt, @updatedBy)"
                                    , pageAnswer);

                return rowsAffected > 0;
            }
        }

        public async Task<bool> InsertGatewayPageAnswerClarification(GatewayPageAnswer pageAnswer, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                var rowsAffected = await connection.ExecuteAsync(
                                    @"INSERT INTO GatewayAnswer ([Id], [ApplicationId], [PageId], [Status], [comments], [UpdatedAt], [UpdatedBy], ClarificationComments, ClarificationDate, ClarificationBy)
														 VALUES (@id, @applicationId, @pageId, @status, @comments, @updatedAt, @updatedBy, @ClarificationComments, @ClarificationDate, @ClarificationBy)"
                                    , pageAnswer);

                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateGatewayPageAnswer(GatewayPageAnswer pageAnswer, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                _logger.LogInformation($"Updating applicationId {pageAnswer.ApplicationId} for non-clarification responses");
                var rowsAffected = await connection.ExecuteAsync(
                                    @"UPDATE GatewayAnswer
                                        SET Status = @status,
                                            Comments = @comments, 
                                            ClarificationDate = CASE WHEN ClarificationAnswer IS NULL THEN NULL ELSE ClarificationDate END,
                                            ClarificationBy = CASE WHEN ClarificationAnswer IS NULL THEN NULL ELSE ClarificationBy END,
                                            ClarificationComments = CASE WHEN ClarificationAnswer IS NULL THEN NULL ELSE ClarificationComments END,
                                            UpdatedBy = @updatedBy,
                                            UpdatedAt = @updatedAt
                                        WHERE [Id] = @id"
                                    , pageAnswer);

                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateGatewayPageAnswerClarification(GatewayPageAnswer pageAnswer, string userId, string userName)
        {
            _logger.LogInformation($"updating Gateway answer for applicationID [{pageAnswer.ApplicationId}], Status: {pageAnswer.Status}, Clarification answer '{pageAnswer.ClarificationAnswer}'");
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                _logger.LogInformation($"Updating applicationId {pageAnswer.ApplicationId} for clarification");
                var rowsAffected = await connection.ExecuteAsync(
                                    @"UPDATE GatewayAnswer
                                         SET Status = @status,
                                             Comments = @clarificationComments,
                                             ClarificationComments = @clarificationComments, 
                                             UpdatedBy = @updatedBy,
                                             UpdatedAt = @updatedAt, 
                                             ClarificationDate = @updatedAt,
                                             ClarificationBy = @updatedBy
                                         WHERE [Id] = @id"
                                    , pageAnswer);

                return rowsAffected > 0;
            }
        }

        public async Task<bool> UpdateGatewayPageAnswerPostClarification(GatewayPageAnswer pageAnswer, string userId, string userName)
        {
            using (var connection = new SqlConnection(_config.SqlConnectionString))
            {
                _logger.LogInformation($"Updating applicationId {pageAnswer.ApplicationId} for non-clarification responses");
                var rowsAffected = await connection.ExecuteAsync(
                                    @"UPDATE GatewayAnswer
                                        SET Status = @status,
                                            Comments = @comments, 
                                            ClarificationAnswer = @clarificationAnswer,
                                            UpdatedBy = @updatedBy,
                                            UpdatedAt = @updatedAt
                                        WHERE [Id] = @id"
                                    , pageAnswer);

                return rowsAffected > 0;
            }
        }

        private string GetOrderByDirection(string sortOrder)
        {
            return "ascending".Equals(sortOrder, StringComparison.InvariantCultureIgnoreCase) ? "ASC" : "DESC";
        }

        private string GetSortColumnForNew()
        {
            return "CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE)";
        }

        private string GetSortColumnForInProgress(string requestedColumn)
        {
            return "LastCheckedBy".Equals(requestedColumn, StringComparison.InvariantCultureIgnoreCase)
                ? "apply.GatewayUserName"
                : "CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE)";
        }

        private string GetSortColumnForOutcome(string requestedColumn)
        {
            return "OutcomeMadeBy".Equals(requestedColumn, StringComparison.InvariantCultureIgnoreCase)
                ? @"CASE 
                WHEN apply.ApplicationStatus = @applicationStatusWithdrawn THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationWithdrawnBy')
                WHEN apply.ApplicationStatus = @applicationStatusRemoved THEN JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationRemovedBy')
                ELSE apply.GatewayUserName
                END"
                : "CAST(JSON_VALUE(apply.ApplyData, '$.ApplyDetails.ApplicationSubmittedOn') AS DATE)";
        }
    }
}

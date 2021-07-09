﻿/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/

-- cleanup (for now)
BEGIN
	DELETE FROM EmailTemplates;
END

IF NOT EXISTS (SELECT * FROM EmailTemplates WHERE TemplateName = N'RoATPGetHelpWithQuestion')
BEGIN
	INSERT INTO EmailTemplates ([Id], [Status], [TemplateName], [TemplateId], [Recipients], [CreatedAt], [CreatedBy]) 
	VALUES (NEWID(), 'Live', N'RoATPGetHelpWithQuestion', N'9d1e1a7e-3557-4781-8901-ea627ae70ec2', N'helpdesk@manage-apprenticeships.service.gov.uk', GETDATE(), 'System')
END

IF NOT EXISTS (SELECT * FROM EmailTemplates WHERE TemplateName = N'RoATPApplicationSubmitted')
BEGIN
	INSERT INTO EmailTemplates ([Id], [Status], [TemplateName], [TemplateId], [CreatedAt], [CreatedBy]) 
	VALUES (NEWID(), 'Live', N'RoATPApplicationSubmitted', N'4a44e79d-1e98-4b90-9d67-f575be97def6', GETDATE(), 'System')
END

IF EXISTS (SELECT * FROM EmailTemplates WHERE TemplateName = N'RoATPApplicationWithdrawn')
BEGIN
	DELETE FROM EmailTemplates where TemplateName = N'RoATPApplicationWithdrawn'
END

IF NOT EXISTS (SELECT * FROM EmailTemplates WHERE TemplateName = N'RoATPApplicationUpdated')
BEGIN
	INSERT INTO EmailTemplates ([Id], [Status], [TemplateName], [TemplateId], [CreatedAt], [CreatedBy]) 
	VALUES (NEWID(), 'Live', N'RoATPApplicationUpdated', N'ebb28424-b1ce-4374-b24b-a240f0cecdc1', GETDATE(), 'System')
END


BEGIN
	DELETE FROM WhitelistedProviders;

	-- APR-2424 Whitelisted Providers May Refresh
	INSERT INTO WhitelistedProviders ([UKPRN], [StartDateTime], [EndDateTime]) 
	VALUES (10002085, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
	       (10007165, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10007177, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10023326, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10024636, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10027061, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10027216, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10029699, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10032315, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10032663, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10033950, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10036126, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10040392, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10040411, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10049431, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10061312, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10062335, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10063769, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10000565, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10000831, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10001113, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10001156, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10001777, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10004486, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10005204, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10019581, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10024704, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10029952, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10031982, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10034969, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10037391, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10061524, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59'),
		   (10013516, N'2021-05-17 08:00:00', N'2021-06-30 23:59:59')

-- APR-2530 Whitelisted Providers July Refresh
	INSERT INTO WhitelistedProviders ([UKPRN], [StartDateTime], [EndDateTime]) 
	VALUES (10034146, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10037203, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10039772, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10047354, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10048055, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10056832, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10057290, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10063352, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10063869, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10067387, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10003375, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10004643, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10025998, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10027272, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10028038, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10034309, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10035789, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10038023, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10038772, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10039527, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10040525, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10042570, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10046692, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10048177, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10052858, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10056912, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10057050, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10061219, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10061407, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10061826, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10061842, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10063274, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10063309, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10065535, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10065578, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10065628, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10065960, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10084622, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10007784, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10019812, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10044886, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10045282, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10047122, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10056428, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10057055, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10065872, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10066838, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10067528, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10040187, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59'),
		   (10055542, N'2021-07-01 08:00:00', N'2021-07-31 23:59:59')
END

-- APR-2494 script to update GatewayReviewStatus Reject to Rejected
-- Can be removed once APR-2494 deployed to prod
  update apply set GatewayReviewStatus='Rejected' where GatewayReviewStatus='Reject'
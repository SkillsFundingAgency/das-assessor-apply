IF NOT EXISTS
	(SELECT 1 FROM [dbo].[Workflows] WHERE [Type] = 'RoATP')
BEGIN
	INSERT INTO [dbo].[Workflows]
           ([Id]
           ,[Description]
           ,[Version]
           ,[Type]
           ,[Status]
           ,[CreatedAt]
           ,[CreatedBy]           
           ,[ReferenceFormat])
     VALUES
           ('86F83D58-8608-4462-9A4E-65837AF04287'
           ,'RoATP Workflow'
           ,'1.0'
           ,'RoATP'
           ,'Live'
           ,GETDATE()
           ,'Import'
           ,'AAD')
END


DELETE FROM [dbo].[WorkflowSequences]
WHERE WorkflowId = '86F83D58-8608-4462-9A4E-65837AF04287'

DELETE FROM [dbo].[WorkflowSections]
WHERE WorkflowId = '86F83D58-8608-4462-9A4E-65837AF04287'

INSERT INTO [dbo].[WorkflowSequences]
           ([Id]
           ,[WorkflowId]
           ,[SequenceId]
           ,[Status]
           ,[IsActive])
     VALUES
           ('79F6D68E-1EC3-47A5-9BAA-4CBE987B3153'
           ,'86F83D58-8608-4462-9A4E-65837AF04287'
           ,1
           ,'Draft'
           ,1)
		   

DELETE FROM Assets WHERE Reference = 'SQ-1-SE-1-APR-PG-1-1';
INSERT INTO Assets
  (Id, Reference, Type, Text, Status, CreatedAt, CreatedBy)
VALUES
  (NEWID(), 'SQ-1-SE-1-APR-PG-1-1', '', 'Provider route', 'Live', GETUTCDATE(), 'Import')
GO
DELETE FROM Assets WHERE Reference = 'SQ-1-SE-1-APR-PG-1-2';
INSERT INTO Assets
  (Id, Reference, Type, Text, Status, CreatedAt, CreatedBy)
VALUES
  (NEWID(), 'SQ-1-SE-1-APR-PG-1-2', '', 'Choose your organisation''s provider route', 'Live', GETUTCDATE(), 'Import')
GO
DELETE FROM Assets WHERE Reference = 'SQ-1-SE-1-APR-PG-1-3';
INSERT INTO Assets
  (Id, Reference, Type, Text, Status, CreatedAt, CreatedBy)
VALUES
  (NEWID(), 'SQ-1-SE-1-APR-PG-1-3', '', '', 'Live', GETUTCDATE(), 'Import')
GO
	
	
INSERT [dbo].[WorkflowSections]
  ([Id], [WorkflowId], [SequenceId], [SectionId], [QnAData], [Title], [LinkTitle], [Status], [DisplayType], [DisallowedOrgTypes])
VALUES
  (N'414FA7FE-FC4D-4141-8E28-48FE34C120B5', N'86F83D58-8608-4462-9A4E-65837AF04287', 1, 1, N'
{
  "Pages": [
    {
      "PageId": "1",
      "SequenceId": "1",
      "SectionId": "1",
      "Title": "SQ-1-SE-1-APR-PG-1-2",
      "LinkTitle": "SQ-1-SE-1-APR-PG-1-1",
      "InfoText": "SQ-1-SE-1-APR-PG-1-3",
      "Details": null,
      "Questions": [
        {
          "QuestionId": "APR-ORG-01",
          "QuestionTag": "organsation-type",
          "Label": "SQ-1-SE-1-APR-PG-1-2",
          "ShortLabel": "SQ-1-SE-1-APR-PG-1-3",
          "QuestionBodyText": "SQ-1-SE-1-APR-PG-1-3",
          "Hint": "SQ-1-SE-1-APR-PG-1-3",
          "Input": {
            "Type": "TabularData",
            "Value": "",
            "Validations": []
          },
          "Order": null
        }
      ],
      "PageOfAnswers": [ {"Answers": [ {"QuestionId": "APR-ORG-01", "Value": "{\"Caption\":\"Caption Text\", \"HeadingTitles\": [ \"Heading 1\", \"Heading 2\" ], DataRows: [ {\"Columns\": [\"Col 1\", \"Col 2\"] }] }" } ]}],
      "Next": [      
        {
          "Action": "NextPage",
          "ReturnId": "3",
          "Condition": null,
          "ConditionMet": false
        }
      ],
      "Complete": false,
      "AllowMultipleAnswers": false,
      "Order": null,
      "Active": true,
      "Visible": true,
      "Feedback": null,
      "HasFeedback": false,
      "NotRequiredOrgTypes": [],
      "BodyText": "SQ-1-SE-1-APR-PG-1-3"
    }
  ]
}
', N'Your organisation', N'Your organisation', N'Draft', N'Pages', N'')



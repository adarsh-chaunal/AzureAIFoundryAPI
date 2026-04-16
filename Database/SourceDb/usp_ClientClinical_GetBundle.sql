SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*
  SOURCE/EHR DATABASE ONLY
  -----------------------
  This script is meant to be run MANUALLY on the Source/EHR database (your imported BACPAC).
  It is NOT part of the migration runner to avoid any impact on the existing production DB.

  Creates/updates:
    dbo.usp_ClientClinical_GetBundle

  Expected objects in Source/EHR DB:
    dbo.Events, dbo.CSNotes, dbo.EventLocations, dbo.EventStatuses, dbo.vPurposes
    dbo.Assessments
    dbo.ClientGoals, dbo.ClientInterventions
    dbo.ClientDocs, dbo.ClientDocTypes
*/

CREATE OR ALTER PROCEDURE dbo.usp_ClientClinical_GetBundle
    @ClientId INT,
    @TakeEvents INT = 200,
    @TakeAssessments INT = 50,
    @TakeGoals INT = 100,
    @TakeInterventions INT = 200,
    @TakeDocs INT = 100
AS
BEGIN
    SET NOCOUNT ON;

    /* ---- Progress notes / encounter notes ---- */
    SELECT TOP (@TakeEvents)
           CAST(N'Progress Note' AS NVARCHAR(100)) AS [Section],
           CONVERT(NVARCHAR(30), e.EventDate, 126) AS EncounterDate,
           CAST(
               CONCAT(
                   N'EventID: ', e.EventID, N'; ',
                   N'Date: ', CONVERT(NVARCHAR(30), e.EventDate, 126), N'; ',
                   N'Purpose: ', COALESCE(p.PurposeDescription, CONCAT(N'PurposeCodeID=', e.PurposeCodeID)), N'; ',
                   N'Location: ', COALESCE(el.EventLocDescription, CONCAT(N'EventLocID=', e.EventLocID)), N'; ',
                   N'Status: ', COALESCE(es.EventStatus, CONCAT(N'EventStatusID=', e.EventStatusID)), N'; ',
                   CASE WHEN cn.Note IS NULL OR LTRIM(RTRIM(cn.Note)) = N'' THEN N'' ELSE CONCAT(N'Note: ', cn.Note, N' ') END,
                   CASE WHEN cn.Interaction IS NULL OR LTRIM(RTRIM(cn.Interaction)) = N'' THEN N'' ELSE CONCAT(N'Interaction: ', cn.Interaction, N' ') END,
                   CASE WHEN cn.RiskComment IS NULL OR LTRIM(RTRIM(cn.RiskComment)) = N'' THEN N'' ELSE CONCAT(N'Risk: ', cn.RiskComment, N' ') END,
                   CASE WHEN e.Comments IS NULL OR LTRIM(RTRIM(e.Comments)) = N'' THEN N'' ELSE CONCAT(N'Comments: ', e.Comments, N' ') END
               ) AS NVARCHAR(MAX)
           ) AS [Content]
    FROM dbo.Events AS e
         LEFT JOIN dbo.CSNotes AS cn
                   ON cn.EventID = e.EventID
         LEFT JOIN dbo.EventLocations AS el
                   ON el.EventLocID = e.EventLocID
         LEFT JOIN dbo.EventStatuses AS es
                   ON es.EventStatusID = e.EventStatusID
         LEFT JOIN dbo.vPurposes AS p
                   ON p.PurposeCodeID = e.PurposeCodeID
    WHERE e.ClientID = @ClientId
    ORDER BY e.EventDate DESC, e.EventID DESC;

    /* ---- Assessments ---- */
    SELECT TOP (@TakeAssessments)
           CAST(N'Assessment' AS NVARCHAR(100)) AS [Section],
           CONVERT(NVARCHAR(30), a.AssessmentDate, 126) AS EncounterDate,
           CAST(
               CONCAT(
                   N'AssessmentID: ', a.AssessmentsID, N'; ',
                   N'Date: ', CONVERT(NVARCHAR(30), a.AssessmentDate, 126), N'; ',
                   CASE WHEN a.ChiefComplaint IS NULL OR LTRIM(RTRIM(a.ChiefComplaint)) = N'' THEN N'' ELSE CONCAT(N'Chief complaint: ', a.ChiefComplaint, N' ') END,
                   CASE WHEN a.BackgroundInfo IS NULL OR LTRIM(RTRIM(a.BackgroundInfo)) = N'' THEN N'' ELSE CONCAT(N'Background: ', a.BackgroundInfo, N' ') END
               ) AS NVARCHAR(MAX)
           ) AS [Content]
    FROM dbo.Assessments AS a
    WHERE a.ClientID = @ClientId
    ORDER BY a.AssessmentDate DESC, a.AssessmentsID DESC;

    /* ---- Goals ---- */
    SELECT TOP (@TakeGoals)
           CAST(N'Goal' AS NVARCHAR(100)) AS [Section],
           NULL AS EncounterDate,
           CAST(
               CONCAT(
                   N'ClientGoalID: ', g.ClientGoalsID, N'; ',
                   COALESCE(CAST(g.ClientGoal AS NVARCHAR(MAX)), N''),
                   CASE
                       WHEN g.ClientGoalDetails IS NULL OR LTRIM(RTRIM(CAST(g.ClientGoalDetails AS NVARCHAR(MAX)))) = N'' THEN N''
                       ELSE CONCAT(N' Details: ', CAST(g.ClientGoalDetails AS NVARCHAR(MAX)))
                   END
               ) AS NVARCHAR(MAX)
           ) AS [Content]
    FROM dbo.ClientGoals AS g
    WHERE g.ClientID = @ClientId
    ORDER BY g.ClientGoalsID DESC;

    /* ---- Interventions ---- */
    SELECT TOP (@TakeInterventions)
           CAST(N'Intervention' AS NVARCHAR(100)) AS [Section],
           NULL AS EncounterDate,
           CAST(
               CONCAT(
                   N'ClientInterventionID: ', i.ClientInterventionsID, N'; ',
                   COALESCE(CAST(i.ClientIntervention AS NVARCHAR(MAX)), N'')
               ) AS NVARCHAR(MAX)
           ) AS [Content]
    FROM dbo.ClientInterventions AS i
    WHERE i.ClientID = @ClientId
    ORDER BY i.ClientInterventionsID DESC;

    /* ---- Documents ---- */
    SELECT TOP (@TakeDocs)
           CAST(N'Document' AS NVARCHAR(100)) AS [Section],
           CONVERT(NVARCHAR(30), d.CreatedDate, 126) AS EncounterDate,
           CAST(
               CONCAT(
                   N'ClientDocsID: ', d.ClientDocsID, N'; ',
                   N'DocTypesID: ', d.ClientDocTypesID, N'; ',
                   CASE WHEN dt.DocType IS NULL OR LTRIM(RTRIM(dt.DocType)) = N'' THEN N'' ELSE CONCAT(N'DocType: ', dt.DocType, N'; ') END,
                   CASE WHEN d.Comments IS NULL OR LTRIM(RTRIM(d.Comments)) = N'' THEN N'' ELSE CONCAT(N'Comments: ', d.Comments, N' ') END
               ) AS NVARCHAR(MAX)
           ) AS [Content]
    FROM dbo.ClientDocs AS d
         LEFT JOIN dbo.ClientDocTypes AS dt
                   ON dt.ClientDocTypesID = d.ClientDocTypesID
    WHERE d.ClientID = @ClientId
    ORDER BY d.CreatedDate DESC, d.ClientDocsID DESC;
END;
GO


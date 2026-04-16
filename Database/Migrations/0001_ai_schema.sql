SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ============================================================
   AI database schema (separate from Source/EHR database)
   Contains ONLY AI request tracking + generated summaries.
   ============================================================ */

IF OBJECT_ID(N'dbo.AIClientSummaryRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIClientSummaryRequest
    (
        RequestId         UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AIClientSummaryRequest PRIMARY KEY,
        ClientId          INT              NOT NULL,
        Status            NVARCHAR(30)      NOT NULL,
        CreatedAtUtc      DATETIME2(3)      NOT NULL CONSTRAINT DF_AIClientSummaryRequest_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        StartedAtUtc      DATETIME2(3)      NULL,
        CompletedAtUtc    DATETIME2(3)      NULL,
        LatestSummaryId   UNIQUEIDENTIFIER  NULL,
        ErrorMessage      NVARCHAR(2000)    NULL
    );

    CREATE INDEX IX_AIClientSummaryRequest_ClientId_CreatedAtUtc
        ON dbo.AIClientSummaryRequest (ClientId, CreatedAtUtc DESC);
END;
GO

IF OBJECT_ID(N'dbo.AIClientSummary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIClientSummary
    (
        SummaryId       UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AIClientSummary PRIMARY KEY,
        RequestId       UNIQUEIDENTIFIER NOT NULL,
        ClientId        INT              NOT NULL,
        SummaryText     NVARCHAR(MAX)    NOT NULL,
        TokensUsed      INT              NULL,
        Model           NVARCHAR(200)    NULL,
        CreatedAtUtc    DATETIME2(3)     NOT NULL CONSTRAINT DF_AIClientSummary_CreatedAtUtc DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_AIClientSummary_ClientId_CreatedAtUtc
        ON dbo.AIClientSummary (ClientId, CreatedAtUtc DESC);

    CREATE INDEX IX_AIClientSummary_RequestId
        ON dbo.AIClientSummary (RequestId);
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AIClientSummaryRequest_Create
    @RequestId UNIQUEIDENTIFIER,
    @ClientId  INT
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.AIClientSummaryRequest (RequestId, ClientId, Status)
    VALUES (@RequestId, @ClientId, N'Queued');
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AIClientSummaryRequest_UpdateStatus
    @RequestId       UNIQUEIDENTIFIER,
    @Status          NVARCHAR(30),
    @ErrorMessage    NVARCHAR(2000) = NULL,
    @LatestSummaryId UNIQUEIDENTIFIER = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.AIClientSummaryRequest
    SET Status = @Status,
        ErrorMessage = @ErrorMessage,
        LatestSummaryId = COALESCE(@LatestSummaryId, LatestSummaryId),
        StartedAtUtc = CASE WHEN @Status = N'Processing' AND StartedAtUtc IS NULL THEN SYSUTCDATETIME() ELSE StartedAtUtc END,
        CompletedAtUtc = CASE WHEN @Status IN (N'Completed', N'Failed') THEN SYSUTCDATETIME() ELSE CompletedAtUtc END
    WHERE RequestId = @RequestId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AIClientSummaryRequest_Get
    @RequestId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RequestId,
           ClientId,
           Status,
           CreatedAtUtc,
           StartedAtUtc,
           CompletedAtUtc,
           LatestSummaryId,
           ErrorMessage
    FROM dbo.AIClientSummaryRequest
    WHERE RequestId = @RequestId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AIClientSummary_Insert
    @RequestId   UNIQUEIDENTIFIER,
    @ClientId    INT,
    @SummaryText NVARCHAR(MAX),
    @TokensUsed  INT = NULL,
    @Model       NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SummaryId UNIQUEIDENTIFIER = NEWID();

    INSERT INTO dbo.AIClientSummary (SummaryId, RequestId, ClientId, SummaryText, TokensUsed, Model)
    VALUES (@SummaryId, @RequestId, @ClientId, @SummaryText, @TokensUsed, @Model);

    SELECT @SummaryId AS SummaryId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_AIClientSummary_ListByClient
    @ClientId INT,
    @Take     INT = 50
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (@Take)
           SummaryId,
           RequestId,
           ClientId,
           CreatedAtUtc,
           TokensUsed,
           Model,
           SummaryText
    FROM dbo.AIClientSummary
    WHERE ClientId = @ClientId
    ORDER BY CreatedAtUtc DESC;
END;
GO


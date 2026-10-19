USE MockModuleDB;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.files_CRUD_JSON
    -- Params
    -- @param1 datatype,
    -- @param2 datatype
    @Action  VARCHAR(12),
    @Payload NVARCHAR(MAX) = N'{}'
AS
BEGIN
    -- SQL_statements to be executed
    SET NOCOUNT ON;
    DECLARE @TenantID CHAR(32) = CONVERT(CHAR(32), SESSION_CONTEXT(N'TenantID'));
    DECLARE @MemberID CHAR(32) = CONVERT(CHAR(32), SESSION_CONTEXT(N'MemberID'));
    IF @TenantID IS NULL OR @MemberID IS NULL
        THROW 50001, 'FILES_CONTEXT_REQUIRED', 1;
    DECLARE @FileID CHAR(32) = JSON_VALUE(@Payload, '$.fileId');

    IF @Action = 'SELECT'
    BEGIN
        SELECT
            id          AS  fileId,
            filename    AS  filename,
            mime_type   AS  mimeType,
            size_bytes  AS  sizeBytes,
            status      AS  status,
            created_at  AS  createdUtc,
            updated_at  AS  updatedUtc
        FROM dbo.files
        WHERE tenant_id = @TenantID
          AND status <> 'DELETED'
        ORDER BY updated_at DESC
        FOR JSON PATH;
    END

    ELSE IF @Action = 'INSERT'
    BEGIN
        SET @FileID = REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');
        INSERT dbo.files (id, tenant_id, owner_id, filename, mime_type, size_bytes, storage_key, status, created_at, updated_at)
        VALUES (
            @FileID,
            @TenantID,
            @MemberID,
            JSON_VALUE(@Payload, '$.filename'),
            JSON_VALUE(@Payload, '$.mimeType'),
            CAST(JSON_VALUE(@Payload, '$.sizeBytes') AS BIGINT),
            @FileID,
            'PENDING',
            SYSUTCDATETIME(),
            SYSUTCDATETIME()
        );

        SELECT @FileID AS fileId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
    END

    ELSE IF @Action = 'DELETE'
    BEGIN
        UPDATE dbo.files
        SET status = 'DELETED', updated_at = SYSUTCDATETIME(), deleted_at = SYSUTCDATETIME()
        WHERE id = @FileID
          AND tenant_id = @TenantID;

        SELECT @FileID AS fileId FOR JSON PATH, WITHOUT_ARRAY_WRAPPER;
    END

    ELSE
        THROW 50002, 'FILES_ACTION_INVALID', 1;
END
GO

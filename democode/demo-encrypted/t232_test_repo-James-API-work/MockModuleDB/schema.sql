CREATE DATABASE MockModuleDB;
GO
USE MockModuleDB;
GO
CREATE SCHEMA rls;
GO

-- User Table
CREATE TABLE users (
    id              CHAR(32)        NOT NULL,
    email           VARCHAR(320)    NOT NULL,
    name            VARCHAR(255)    NOT NULL,
    password_hash   VARCHAR(255),
    created_at      DATETIME2       NOT NULL,
    updated_at      DATETIME2       NOT NULL,

    CONSTRAINT pk_users         PRIMARY KEY (id),
    CONSTRAINT uq_users_email   UNIQUE      (email)
);

-- Tenants
CREATE TABLE tenants (
    id          CHAR(32)        NOT NULL,
    name        VARCHAR(255)    NOT NULL,
    created_at  DATETIME2       NOT NULL,
    updated_at  DATETIME2       NOT NULL,

    CONSTRAINT  pk_tenants  PRIMARY KEY (id)
);

-- Tenant Memberships
CREATE TABLE tenant_memberships (
    id          CHAR(32)    NOT NULL,
    user_id     CHAR(32)    NOT NULL,
    tenant_id   CHAR(32)    NOT NULL,
    joined_at   DATETIME2   NOT NULL,

    CONSTRAINT pk_tenant_memberships PRIMARY KEY (id),
    CONSTRAINT uq_user_tenant UNIQUE (user_id, tenant_id),
    CONSTRAINT fk_membership_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    CONSTRAINT fk_membership_tenant
        FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE
);

-- Files
CREATE TABLE files  (
    id          CHAR(32)        NOT NULL,
    tenant_id   CHAR(32)        NOT NULL,
    owner_id    CHAR(32)        NOT NULL,
    filename    VARCHAR(512)    NOT NULL,
    mime_type   VARCHAR(255)    NOT NULL,
    size_bytes  BIGINT          NOT NULL,
    storage_key VARCHAR(1024)   NOT NULL,
    status      VARCHAR(20)     NOT NULL,
    created_at  DATETIME2       NOT NULL,
    updated_at  DATETIME2       NOT NULL,
    deleted_at  DATETIME2,

    CONSTRAINT pk_files PRIMARY KEY (id),
    CONSTRAINT uq_files_storage_key UNIQUE (storage_key),
    CONSTRAINT ck_files_status CHECK (status IN ('PENDING', 'UPLOADED', 'FAILED', 'DELETED')),
    CONSTRAINT fk_files_tenant
        FOREIGN KEY (tenant_id) REFERENCES tenants(id) ON DELETE CASCADE,
    CONSTRAINT fk_files_owner
        FOREIGN KEY (owner_id) REFERENCES users(id)
);

-- File contents
CREATE TABLE file_contents (
    file_id             CHAR(32)       	NOT NULL,
    tenant_id   	    CHAR(32)	    NOT NULL,
    ciphertext          VARBINARY(MAX) 	NOT NULL,
    nonce               VARBINARY(16)  	NOT NULL,
    auth_tag            VARBINARY(16)  	NOT NULL,
    wrapped_dek         VARBINARY(512) 	NOT NULL,
    plaintext_checksum  VARCHAR(128)   	NOT NULL,

    CONSTRAINT pk_file_contents PRIMARY KEY (file_id),
    CONSTRAINT fk_file_contents_tenant
        FOREIGN KEY (tenant_id) REFERENCES tenants(id),
    CONSTRAINT fk_file_contents_file
        FOREIGN KEY (file_id) REFERENCES files(id) ON DELETE CASCADE
);

GO

-- RLS predicate function
CREATE FUNCTION rls.fn_TenantAccessPredicate(@TenantId CHAR(32)) 
RETURNS TABLE 
WITH SCHEMABINDING 
AS 
RETURN SELECT 1 AS AccessResult 
WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantID') AS CHAR(32));
GO

-- Security Policy
CREATE SECURITY POLICY rls.FilesTenantPolicy 
ADD FILTER  PREDICATE rls.fn_TenantAccessPredicate(tenant_id) ON dbo.files, 
ADD BLOCK   PREDICATE rls.fn_TenantAccessPredicate(tenant_id) ON dbo.files AFTER INSERT, 
ADD BLOCK   PREDICATE rls.fn_TenantAccessPredicate(tenant_id) ON dbo.files AFTER UPDATE, 
ADD FILTER  PREDICATE rls.fn_TenantAccessPredicate(tenant_id) ON dbo.file_contents, 
ADD BLOCK   PREDICATE rls.fn_TenantAccessPredicate(tenant_id) ON dbo.file_contents AFTER INSERT, 
ADD BLOCK   PREDICATE rls.fn_TenantAccessPredicate(tenant_id) ON dbo.file_contents AFTER UPDATE 
WITH (STATE = ON);

-- ===== Create or Alter procedures =====
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

--
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

--
CREATE OR ALTER PROCEDURE dbo.files_Content_Put
    -- Params
    -- @param1 datatype,
    -- @param2 datatype
    @FileID      CHAR(32),
    @Ciphertext  VARBINARY(MAX),
    @Nonce       VARBINARY(16),
    @AuthTag     VARBINARY(16),
    @WrappedDek  VARBINARY(512),
    @Checksum    VARCHAR(128)
AS
BEGIN
    -- SQL_statements to be executed
    SET NOCOUNT ON;
    DECLARE @TenantID CHAR(32) = CONVERT(CHAR(32), SESSION_CONTEXT(N'TenantID'));
    IF @TenantID IS NULL
        THROW 50001, 'FILES_CONTEXT_REQUIRED', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.files WHERE id = @FileID AND tenant_id = @TenantID)
        THROW 50003, 'FILES_NOT_FOUND', 1;

    INSERT dbo.file_contents (file_id, tenant_id, ciphertext, nonce, auth_tag, wrapped_dek, plaintext_checksum)
    VALUES (@FileID, @TenantID, @Ciphertext, @Nonce, @AuthTag, @WrappedDek, @Checksum);

    UPDATE dbo.files SET status = 'UPLOADED', updated_at = SYSUTCDATETIME() WHERE id = @FileID;
END
GO

-- 
CREATE OR ALTER PROCEDURE dbo.files_Content_Get
    -- Params
    -- @param1 datatype,
    -- @param2 datatype
    @FileID CHAR(32)
AS
BEGIN
    -- SQL_statements to be executed
    SET NOCOUNT ON;
    DECLARE @TenantID CHAR(32) = CONVERT(CHAR(32), SESSION_CONTEXT(N'TenantID'));
    IF @TenantID IS NULL
        THROW 50001, 'FILES_CONTEXT_REQUIRED', 1;

    SELECT fc.ciphertext, fc.nonce, fc.auth_tag, fc.wrapped_dek, fc.plaintext_checksum, f.filename
    FROM dbo.file_contents fc
    JOIN dbo.files f ON f.id = fc.file_id
    WHERE fc.file_id = @FileID
      AND fc.tenant_id = @TenantID
      AND f.status = 'UPLOADED';
END
GO


-- ===== Test data =====
-- Tenants
INSERT INTO tenants (id, name, created_at, updated_at) VALUES
('00000000000000000000000000000001', 'Tenant A', SYSUTCDATETIME(), SYSUTCDATETIME()),
('00000000000000000000000000000002', 'Tenant B', SYSUTCDATETIME(), SYSUTCDATETIME());

-- Users
INSERT INTO users (id, email, name, password_hash, created_at, updated_at) VALUES
('00000000000000000000000000000001', 'andy@example.com', 'Andy', NULL, SYSUTCDATETIME(), SYSUTCDATETIME()),
('00000000000000000000000000000002', 'bob@example.com',   'Bob',   NULL, SYSUTCDATETIME(), SYSUTCDATETIME());

-- Tenant memberships
INSERT INTO tenant_memberships (id, user_id, tenant_id, joined_at) VALUES
('00000000000000000000000000000003', '00000000000000000000000000000001', '00000000000000000000000000000001', SYSUTCDATETIME()),
('00000000000000000000000000000004', '00000000000000000000000000000002', '00000000000000000000000000000002', SYSUTCDATETIME());

-- Files: bypass RLS for testing only
EXEC sys.sp_set_session_context N'TenantID', '00000000000000000000000000000001';
INSERT INTO files (id, tenant_id, owner_id, filename, mime_type, size_bytes, storage_key, status, created_at, updated_at) VALUES
('00000000000000000000000000000005', '00000000000000000000000000000001', '00000000000000000000000000000001', 'andy.txt', 'text/plain', 13, '00000000000000000000000000000005', 'UPLOADED', SYSUTCDATETIME(), SYSUTCDATETIME());
INSERT INTO file_contents (file_id, tenant_id, ciphertext, nonce, auth_tag, wrapped_dek, plaintext_checksum) VALUES
('00000000000000000000000000000005', '00000000000000000000000000000001', 0x1234, 0x00000000000000000000000000000000, 0x00000000000000000000000000000000, 0x00,  'dummy-checksum-a');
EXEC sys.sp_set_session_context N'TenantID', '00000000000000000000000000000002';
INSERT INTO files (id, tenant_id, owner_id, filename, mime_type, size_bytes, storage_key, status, created_at, updated_at) VALUES
('00000000000000000000000000000006', '00000000000000000000000000000002', '00000000000000000000000000000002', 'bobs-bananas.txt', 'text/plain', 9, '00000000000000000000000000000006', 'UPLOADED', SYSUTCDATETIME(), SYSUTCDATETIME());
INSERT INTO file_contents (file_id, tenant_id, ciphertext, nonce, auth_tag, wrapped_dek, plaintext_checksum) VALUES
('00000000000000000000000000000006', '00000000000000000000000000000002', 0x5678, 0x00000000000000000000000000000000, 0x00000000000000000000000000000000, 0x00, 'dummy-checksum-b');
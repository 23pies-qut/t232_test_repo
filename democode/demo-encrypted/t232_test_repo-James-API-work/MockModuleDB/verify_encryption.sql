-- Shows how the newest uploads are stored in file_contents (Tenant A / Andy).
USE MockModuleDB;
SET NOCOUNT ON;

-- RLS: nothing is visible until the tenant is set on this connection.
EXEC sys.sp_set_session_context N'TenantID', '00000000000000000000000000000001';

SELECT TOP 5
    f.filename,
    f.size_bytes,
    DATALENGTH(fc.ciphertext)  AS cipher_bytes,
    DATALENGTH(fc.nonce)       AS nonce_bytes,
    DATALENGTH(fc.auth_tag)    AS tag_bytes,
    DATALENGTH(fc.wrapped_dek) AS dek_bytes,
    fc.plaintext_checksum,
    LEFT(CONVERT(VARCHAR(MAX), fc.ciphertext, 1), 42) AS ciphertext_start
FROM dbo.file_contents fc
JOIN dbo.files f ON f.id = fc.file_id
ORDER BY f.created_at DESC;

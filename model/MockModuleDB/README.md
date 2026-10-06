# Database schema/build file explained

This script builds a **multi-tenant encrypted file storage** database in SQL Server. Multiple tenants (organizations) share one database, and **Row-Level Security (RLS)** ensures each tenant can only see its own files.

In the real system, users, tenants and memberships are owned by other microservices and thus in this file they are for testing only. files and file_contents will be the only tables that belong to this service and are protected by a crude RLS. 


## How the pieces fit together

A file belongs to one tenant and is owned by one user, both are referenced by ID only. The files encrypted content lives in a separate table for security and so metadata remains independently queryable.

## Tables

Tables 'users', 'tenants' and 'tenant_memberships' exist only so we can test locally. They will be removed and replaced by calls to the user and tenant services in prod.


### files

The purpose is to store each file's metadata. 

files:
    'id'            =>  a file's unique identifier for lookups (matches file_contents table - fc for short)
    'tenant_id'     =>  file's tenant owner (matches fc)
    'owner_id'      =>  file's user owner (matches fc)
    'filename'      =>  file's name (result of c#'s file.name)
    'mime_type'     =>  content type (like extension, the result of c#'s file.type)
    'size_bytes'    =>  file's byte size (result of c#'s file.size)
    'storage_key'   =>  future blob storage (Binary Large Object) placeholder 
    'status'        =>  stored status of the file, one of 'PENDING', 'UPLOADED', 'FAILED' or 'DELETED'.
    'updated_at'    =>  for sorting results
    'updated_at'    =>  for sorting results
    'deleted_at'    =>  for now files aren't truly deleted, only hidden


### file_contents
Encryption resource:
https://medium.com/@pravallikayakkala123/understanding-aes-encryption-and-aes-gcm-mode-an-in-depth-exploration-using-java-e03be85a3faa

The purpose is to store the encrypted file itself using AES-GCM.
Advanced Encryption Standard Galois/Counter Mode.

file_contents:
    'id'                    =>  a file's unique identifer for lookups (matches files table)
    'tenant_id'             =>  only tenant_id so RLS can filter this table independently from files, avoiding table joins 
    'ciphertext'            =>  encrypted data
    'nonce'                 =>  initialization vector (IV) needed for decryption
    'auth_tag'              =>  similar to checksum, produced by AES/GCM for storage level tampering check
    'wrapped_dek'           =>  dek = data encryption key, it's wrapped because this key has also been encrypted. 
    'plaintext_checksum'    =>  a hash of the original unencrypted file. Ensures decryption is successful (end to end)


## Row-Level Security (RLS)
Handy resources:
https://www.sqlservercentral.com/stairways/stairway-to-sql-server-security
https://www.sqlservercentral.com/stairways/stairway-to-row-level-security

Session context resouce:
https://www.sqlshack.com/sql-server-session_context-function-with-examples/

RLS predicate function resouce:
https://www.sqlservercentral.com/steps/row-level-security-predicate-functions-level-2-of-the-stairway-to-row-level-security

Security policy resource:
https://www.sqlshack.com/filter-block-data-access-using-sql-server-2016-row-level-security/

RLS makes the database itself enforce tenant isolation, instead of adding 'WHERE tenant_id = EXAMPLE_ID' to queries.
I don't want to pretend I fully understand everything thats going on here but I used these resources a lot.

### My breakdown:
It has three parts:

#### 1. Session Context
The app tells SQL Server who is making the request by setting a value on the connection:

```sql
EXEC sys.sp_set_session_context N'TenantID', '<tenant-id>';
```

#### 2. Predicate function
rls.fn_TenantAccessPredicate returns a row only when the row's tenant_id matches the session's TenantID.

```sql
RETURN SELECT 1 AS AccessResult
WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantID') AS CHAR(36));
```

#### 3. Security Policy
rls.FilesTenantPolicy maps that function to files table and file_contents table:

ADD FILTER              => Filters rows from other tenants to be invisible to queries like SELECT, UPDATE, and DELETE.
ADD BLOCK AFTER INSERT  => You can't insert a row for a different tenant 
ADD BLOCK AFTER UPDATE  => You can't change a row's tenant_id to another tenant 

This pretty much means that if TenantID is set to Tenant A, 'SELECT * FROM files' returns only Tenant A's files, even though Tenant B's rows are in the same table. The tenantId of the row (file) being added is checked against the tenantId from session context and blocks mismatches. 

## Stored Procedures
resources:
https://www.w3schools.com/sql/sql_stored_procedures.asp
https://www.mssqltips.com/sqlservertip/4640/create-or-alter-statement-in-sql-server/
https://www.c-sharpcorner.com/blogs/sending-json-to-a-stored-procedure-and-handling-it-in-sql-server

All data access for files goes through stored procedures. Each one reads TenantID from the session context and throws an error if it's missing.

### files_CRUD_JSON
A single procedure handling basic file metadata operations, with JSON in and out.

```sql
EXEC dbo.files_CRUD_JSON @Action = 'SELECT';

EXEC dbo.files_CRUD_JSON
     @Action  = 'INSERT',
     @Payload = N'{"filename":"report.pdf","mimeType":"application/pdf","sizeBytes":20480}';

EXEC dbo.files_CRUD_JSON
     @Action  = 'DELETE',
     @Payload = N'{"fileId":"<file-id>"}';
```

### files_Content_Put
Stores the encrypted content for a file. It verifies the file exists for the current tenant, inserts the file_contents row, and flips the file's status to UPLOADED.

### files_Content_Get
Returns the encrypted content and the crypto metadata needed to decrypt it. It only returns content for files with status UPLOADED.

### Typical upload flow

1. files_CRUD_JSON INSERT    => status PENDING, returns fileId
2. App encrypts the file
3. files_Content_Put         => stores ciphertext, status becomes UPLOADED
4. files_Content_Get         => app fetches ciphertext and decrypts it


## Test Data

The bottom of the script seeds two tenants, each with one user and one file. The ciphertext values are dummy bytes (`0x1234`). The script sets TenantID before each file insert so it satisfies the BLOCK AFTER INSERT predicate so the rows are allowed in.

To verify RLS works, set the session to one tenant and select:

```sql
EXEC sys.sp_set_session_context N'TenantID', '00000000000000000000000000000001';
SELECT * FROM files;
```
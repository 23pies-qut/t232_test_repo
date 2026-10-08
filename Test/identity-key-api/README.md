# Identity Key API

A .NET 9 Web API that stores uploaded files encrypted in SQL Server.

The frontend never touches a key. The API encrypts the file before saving it,
so the database only ever contains ciphertext.

## Built with

- .NET 9 (ASP.NET Core Web API)
- Microsoft.Data.SqlClient, calling stored procedures
- SQL Server

## How the encryption works

There are two keys:

- **KEK** - the master key, 32 bytes, kept outside the repository
- **DEK** - a random key generated for every file

The file is encrypted with AES-256-GCM using its own DEK. That DEK is then
encrypted with the KEK and stored next to the file. This is called envelope
encryption, and the point of it is that changing the master key does not mean
re-encrypting every file - you only re-wrap the small DEKs.

Downloading reverses it: read the wrapped DEK, unwrap it with the KEK, then
decrypt the file.

The SHA-256 checksum stored with each file is only there to catch corruption
(a bad backup, a disk error). The real tamper protection comes from the GCM
authentication tag.

## Project structure

```
identity-key-api/
├─ Program.cs                      setup: DI, CORS, middleware, startup checks
├─ Controllers/FilesController.cs  the four endpoints
├─ Middleware/                     checks the X-Tenant-ID / X-Member-ID headers
├─ Services/
│  ├─ EncryptionService.cs         AES-256-GCM encryption
│  ├─ FileService.cs               talks to SQL Server
│  └─ TenantContextService.cs      tenant + member for the current request
├─ Models/                         request and response shapes
└─ appsettings.json                connection string and settings
```

## Setup

### 1. SQL Server

It needs TCP/IP enabled on port 1433, and a database called `IdentityKeyFiles`
with the tables and stored procedures from `Database/IdentityKeyFiles.sql`.

Check the port is open:

```powershell
netstat -ano | findstr ":1433"
```

If nothing shows up, enable TCP/IP in SQL Server Configuration Manager
(SQL Server Network Configuration -> protocols for MSSQLSERVER -> TCP/IP ->
IPAll -> TCP Port 1433), then restart the SQL Server service.

### 2. The encryption key

The key is not in the repository. Create one and store it with user secrets:

```powershell
cd identity-key-api

$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "Encryption:KeyEncryptionKey" "$key"
dotnet user-secrets list
```

User secrets are stored in `%APPDATA%\Microsoft\UserSecrets\`, outside the
project folder, so the key never ends up in Git.

### 3. Run it

```powershell
dotnet run
```

It should print:

```
Now listening on: http://localhost:5001
```

If the key is missing the app stops straight away with a message telling you
what to do, instead of starting up and failing later.

## Endpoints

Every `/api/files` request needs two headers:

- `X-Tenant-ID` - a GUID
- `X-Member-ID` - a GUID

| Method | Route | Does |
| --- | --- | --- |
| GET | `/api/files` | list the files for this tenant |
| POST | `/api/files` | upload a file (form field `file`, max 25 MB) |
| GET | `/api/files/{id}/download` | download and decrypt a file |
| DELETE | `/api/files/{id}` | mark a file as deleted |

Trying it by hand:

```powershell
curl.exe http://localhost:5001/api/files `
  -H "X-Tenant-ID: 00000000000000000000000000000001" `
  -H "X-Member-ID: 00000000000000000000000000000001"
```

## Where the data goes

`files` stores the metadata: name, MIME type, size, status, and who owns it.

`file_contents` stores the encrypted payload: `ciphertext`, `nonce`,
`auth_tag`, `wrapped_dek` and `plaintext_checksum`.

All reads and writes go through three stored procedures, and they all take the
tenant from `SESSION_CONTEXT`, which the API sets on every connection. Row
level security is switched on for both tables as well, so a query that forgets
`WHERE tenant_id = ...` still cannot see another tenant's rows.

## Notes

- **Changing the key makes old files unreadable.** The wrapped DEK is bound to
  the KEK, so there is no way to recover old files. Delete them and re-upload.
- Logging goes to the console only. The Windows EventLog provider throws an
  access denied error on some machines, and that kills the whole process.
- The multipart memory buffer threshold is raised to 30 MB, otherwise uploads
  over 64 KB get written to a temp file first, which this machine refuses.

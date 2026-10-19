# How to run the encrypted demo

Uploaded files are encrypted with AES-256-GCM (a random key per file, wrapped by a
key-encryption key) before they are stored in `file_contents`, and decrypted on download.

## Requirements

- .NET 10 SDK (`dotnet --version` shows 10.x)
- SQL Server LocalDB (`sqllocaldb info` lists `MSSQLLocalDB`)
- sqlcmd, or SSMS / the VS Code "SQL Server (mssql)" extension

## 1. Create the database (once)

Run from this folder (the repository root):

```powershell
sqllocaldb start MSSQLLocalDB
sqlcmd -S "(localdb)\MSSQLLocalDB" -i "MockModuleDB\schema.sql"
```

If you already have an older `MockModuleDB`, stop the server and drop it first:

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "DROP DATABASE MockModuleDB"
```

(Or keep it and only apply the INSERT fix: `sqlcmd -S "(localdb)\MSSQLLocalDB" -i "MockModuleDB\fix_files_CRUD_JSON.sql"`.)

## 2. Start the server

```powershell
cd MockModule.Server
dotnet run --launch-profile http
```

Open http://localhost:5000. Visual Studio (F5) also works.

## 3. Check it works

1. Upload a new file (simple name, e.g. `test.txt`), then download it - contents match.
2. Run `sqlcmd -S "(localdb)\MSSQLLocalDB" -i "MockModuleDB\verify_encryption.sql" -W -s " | "`
   from the repository root: nonce = 12 bytes, tag = 16, wrapped DEK = 60, ciphertext unreadable.

The seeded files (`andy.txt`, `bobs-bananas.txt`) hold dummy bytes, so downloading them
returns `FILE_DECRYPTION_FAILED`. That is expected.

## Keys - DEV ONLY

`MockModule.Server/appsettings.Development.json` contains a development KEK
(`Encryption:KeyEncryptionKey`) and JWT signing key (`MockModuleSecurity:SigningKeyBase64`)
so the team can run the demo without setup. 
Everyone sharing one database must use the same KEK, or they cannot decrypt each other's files.

<#
  Builds MockModuleDB: (1) create the database (2) run schema.sql (skipped if installed) (3) run seed_multitenant.sql
  Usage: powershell -ExecutionPolicy Bypass -File .\MockModuleDB\setup-database.ps1 [-Server "localhost\SQLEXPRESS"] [-Reset]
  Requires sqlcmd (installed with SQL Server / SSMS) and Windows authentication. -Reset drops the whole database first.
#>
param([string]$Server = "localhost", [switch]$Reset)
$ErrorActionPreference = "Stop"
$Db = "MockModuleDB"

function Sql([string[]]$Arguments) {
    & sqlcmd -S $Server -E -b @Arguments
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed (exit code $LASTEXITCODE); see the error above." }
}

if ($Reset) {
    Write-Host "Dropping $Db ..." -ForegroundColor Yellow
    Sql @("-Q", "IF DB_ID(N'$Db') IS NOT NULL BEGIN ALTER DATABASE [$Db] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$Db]; END")
}

Write-Host "1/3 Creating database $Db ($Server)"
Sql @("-Q", "IF DB_ID(N'$Db') IS NULL CREATE DATABASE [$Db];")

# schema.sql can only run once on an empty database; skip it if already installed
$installed = (& sqlcmd -S $Server -E -d $Db -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN OBJECT_ID(N'dbo.tenants') IS NULL THEN 0 ELSE 1 END;" | Out-String).Trim()
if ($installed -eq "1") { Write-Host "2/3 Schema already installed, skipped (use -Reset to rebuild)" -ForegroundColor Green }
else { Write-Host "2/3 Running schema.sql"; Sql @("-d", $Db, "-i", (Join-Path $PSScriptRoot "schema.sql")) }

Write-Host "3/3 Running seed_multitenant.sql (multi-tenant demo data)"
Sql @("-d", $Db, "-i", (Join-Path $PSScriptRoot "seed_multitenant.sql"))
Write-Host "Done." -ForegroundColor Green

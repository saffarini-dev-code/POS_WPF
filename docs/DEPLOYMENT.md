# POS_WPF Deployment Runbook

## Runtime

- Windows x64
- .NET 10 self-contained production publish
- One WPF executable/application project

## First installation

1. Extract the published package to the application directory.
2. Keep the application directory writable only where required by the deployment policy.
3. Start `POS_WPF.exe`.
4. The application creates the configured local database and seeds the first-run Super Administrator.
5. Sign in with the first-run credentials shown by the login screen and immediately replace the initial password.

## Database providers

### SQLite

Default configuration is local/offline SQLite:

`Data Source=pos-local.db`

### SQL Server

Set `Database:Provider` to `SqlServer` and provide the SQL Server connection string through deployment configuration. SQL Server retry resiliency is enabled.

## Backup

Use the built-in `DatabaseBackupService` for SQLite backups. Store backups outside the application directory and retain multiple generations according to the business recovery policy.

## Synchronization

Set `Synchronization:Enabled=true` and configure `Synchronization:Endpoint` only after the remote synchronization endpoint implements the documented batch/conflict contract.

## Printing

Configure Windows printers by their installed queue name. Receipt defaults to 80mm. Label defaults are 30mm × 20mm with 5mm gap.

## Production gate

Do not deploy until the production readiness matrix is fully checked and the GitHub Actions release workflow produces a verified publish artifact.

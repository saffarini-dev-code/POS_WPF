using Microsoft.Data.Sqlite;

namespace POS_WPF.Infrastructure.Backup;

public sealed class DatabaseBackupService
{
    public async Task BackupSqliteAsync(string sourceDatabase, string destinationFile, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationFile))!);
        await using var source = new SqliteConnection($"Data Source={sourceDatabase}");
        await using var destination = new SqliteConnection($"Data Source={destinationFile}");
        await source.OpenAsync(cancellationToken);
        await destination.OpenAsync(cancellationToken);
        source.BackupDatabase(destination);
    }
}

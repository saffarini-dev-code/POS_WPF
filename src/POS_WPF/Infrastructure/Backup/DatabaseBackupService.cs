using Microsoft.Data.Sqlite;

namespace POS_WPF.Infrastructure.Backup;

public sealed class DatabaseBackupService
{
    public async Task BackupSqliteAsync(string sourceDatabase, string destinationFile, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationFile))!); await using var source = new SqliteConnection($"Data Source={sourceDatabase}"); await using var destination = new SqliteConnection($"Data Source={destinationFile}"); await source.OpenAsync(cancellationToken); await destination.OpenAsync(cancellationToken); source.BackupDatabase(destination);
    }
    public Task RestoreSqliteAsync(string backupFile, string destinationDatabase, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(backupFile)) throw new FileNotFoundException("Backup file was not found.", backupFile); cancellationToken.ThrowIfCancellationRequested(); Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destinationDatabase))!); File.Copy(backupFile, destinationDatabase, true); return Task.CompletedTask;
    }
    public void ApplyRetention(string directory, int retentionDays)
    {
        if (retentionDays < 1) throw new ArgumentOutOfRangeException(nameof(retentionDays)); if (!Directory.Exists(directory)) return; var cutoff = DateTime.UtcNow.AddDays(-retentionDays); foreach (var file in Directory.EnumerateFiles(directory, "*.db")) if (File.GetLastWriteTimeUtc(file) < cutoff) File.Delete(file);
    }
}

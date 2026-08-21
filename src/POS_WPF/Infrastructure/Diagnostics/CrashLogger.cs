namespace POS_WPF.Infrastructure.Diagnostics;

public sealed class CrashLogger
{
    private readonly string _directory;
    public CrashLogger()
    {
        _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "POS_WPF", "Logs");
        Directory.CreateDirectory(_directory);
    }
    public void Log(Exception exception, string source)
    {
        try { File.AppendAllText(Path.Combine(_directory, "application.log"), $"[{DateTime.UtcNow:O}] [{source}] {exception}\n"); } catch { }
    }
}

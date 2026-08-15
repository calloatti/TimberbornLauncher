using System;
using System.IO;
using System.Text;

namespace TimberbornLauncher;

/// <summary>
/// Simple file logger to TimberbornLauncher.log next to the executable.
/// </summary>
public static class Log
{
    private static readonly string LogPath = Path.Combine(
        AppContext.BaseDirectory, "TimberbornLauncher.log");
    private static readonly object Lock = new();

    static Log()
    {
        try
        {
            if (File.Exists(LogPath))
            {
                File.Delete(LogPath);
            }
        }
        catch { }
    }

    public static void Info(string message)
    {
        Write($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}");
        Console.WriteLine($"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}");
    }

    public static void Debug(string message)
    {
        Write($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}");
        Console.WriteLine($"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}");
    }

    public static void Error(string message, Exception? ex = null)
    {
        var sb = new StringBuilder();
        sb.Append($"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}");
        if (ex != null)
        {
            sb.Append($"\n  {ex.GetType().Name}: {ex.Message}");
            sb.Append($"\n  {ex.StackTrace}");
        }
        Write(sb.ToString());
    }

    public static void Launch(string executable, string workingDir, string arguments)
    {
        Write($"[LAUNCH] {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        Write($"  Executable: {executable}");
        Write($"  WorkingDir: {workingDir}");
        Write($"  Arguments: {arguments}");
    }

    private static void Write(string line)
    {
        lock (Lock)
        {
            try
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
                Console.WriteLine(line);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOG FAIL] {ex.Message}");
            }
        }
    }

    public static void Clear()
    {
        lock (Lock)
        {
            try
            {
                if (File.Exists(LogPath))
                {
                    File.Delete(LogPath);
                }
            }
            catch { }
        }
    }
}



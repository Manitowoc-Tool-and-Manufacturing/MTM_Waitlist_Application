using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace MTM_Waitlist_Server.Admin.Logging;

/// <summary>
/// Lightweight, dependency-free logger used exclusively during the startup sequence,
/// before the DI container and ASP.NET logging pipeline are available.
///
/// Writes every entry to:
///   1. A dated file: %LOCALAPPDATA%\MTM_Waitlist_Server\Logs\startup_YYYY-MM-DD.log
///   2. The VS / WinDbg debug output channel (Debug.WriteLine).
///
/// All methods are thread-safe (the underlying StreamWriter is flushed after every line).
/// </summary>
internal static class StartupLogger
{
    private static readonly string _logPath;
    private static readonly object _lock = new();

    static StartupLogger()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MTM_Waitlist_Server", "Logs");

        Directory.CreateDirectory(dir);

        var fileName = $"startup_{DateTime.Now:yyyy-MM-dd}.log";
        _logPath = Path.Combine(dir, fileName);

        // Write a session separator so multiple runs in a day are distinguishable.
        WriteRaw($"{'=',60}");
        WriteRaw($"  SESSION START  {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        WriteRaw($"{'=',60}");
    }

    // ── Public surface ────────────────────────────────────────────────────────

    /// <summary>Logs an informational message.</summary>
    public static void Info(string message,
        [CallerMemberName] string member = "",
        [CallerFilePath]   string file   = "",
        [CallerLineNumber] int    line   = 0)
        => Write("INFO ", message, member, file, line);

    /// <summary>Logs a warning message.</summary>
    public static void Warn(string message,
        [CallerMemberName] string member = "",
        [CallerFilePath]   string file   = "",
        [CallerLineNumber] int    line   = 0)
        => Write("WARN ", message, member, file, line);

    /// <summary>Logs an error with an optional exception.</summary>
    public static void Error(string message, Exception? ex = null,
        [CallerMemberName] string member = "",
        [CallerFilePath]   string file   = "",
        [CallerLineNumber] int    line   = 0)
    {
        Write("ERROR", message, member, file, line);
        if (ex is not null)
        {
            WriteRaw($"           Exception : {ex.GetType().FullName}: {ex.Message}");
            WriteRaw($"           Stack     : {ex.StackTrace?.Replace(Environment.NewLine, Environment.NewLine + "                       ")}");
        }
    }

    /// <summary>Logs a section header to visually group related log lines.</summary>
    public static void Section(string title)
    {
        WriteRaw(string.Empty);
        WriteRaw($"  ── {title} ──");
    }

    /// <summary>Returns the full path of the log file for the current session.</summary>
    public static string LogFilePath => _logPath;

    // ── Private helpers ───────────────────────────────────────────────────────

    private static void Write(string level, string message, string member, string file, int line)
    {
        var shortFile = Path.GetFileNameWithoutExtension(file);
        var entry = $"{DateTime.Now:HH:mm:ss.fff} [{level}] {shortFile}.{member}:{line}  {message}";
        WriteRaw(entry);
    }

    private static void WriteRaw(string line)
    {
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // Log file write failures must never crash the application.
            }

            Debug.WriteLine($"[MTM-Startup] {line}");
        }
    }
}

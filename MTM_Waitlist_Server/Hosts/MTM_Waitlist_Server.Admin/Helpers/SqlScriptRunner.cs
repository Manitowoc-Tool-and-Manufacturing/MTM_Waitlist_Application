using MySqlConnector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Admin.Helpers;

/// <summary>
/// Loads and executes a multi-statement SQL script that uses DELIMITER directives
/// (i.e. scripts written for the mysql CLI tool) via a <see cref="MySqlConnection"/>.
/// MySqlConnector does not understand DELIMITER, so this helper strips the directives
/// and splits the script into individual statements before execution.
/// </summary>
internal static class SqlScriptRunner
{
    /// <summary>
    /// Reads the embedded SQL resource <paramref name="logicalName"/>, strips
    /// DELIMITER directives, splits into individual statements, and executes each
    /// one in order, skipping <c>USE `database`</c> (the connection already targets
    /// the correct schema).
    /// </summary>
    /// <param name="connection">An open connection with the target database already selected.</param>
    /// <param name="logicalName">Logical name of the embedded resource, e.g. <c>V001__Initial_Schema.sql</c>.</param>
    /// <param name="progress">Optional callback invoked after each statement with a short summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of summary strings for each executed statement.</returns>
    public static async Task<List<string>> RunEmbeddedScriptAsync(
        MySqlConnection connection,
        string logicalName,
        Action<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sql = LoadEmbeddedResource(logicalName);
        var statements = SplitStatements(sql);
        var log = new List<string>();

        foreach (var statement in statements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmed = statement.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) { continue; }

            // Skip the USE statement — the connection already targets the right schema.
            if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)) { continue; }

            // Skip the CREATE DATABASE line — Step 1 already created it.
            if (trimmed.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)) { continue; }

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = trimmed;
            await cmd.ExecuteNonQueryAsync(cancellationToken);

            var summary = trimmed.Length > 80
                ? trimmed[..80].Replace('\n', ' ').Replace('\r', ' ') + "…"
                : trimmed.Replace('\n', ' ').Replace('\r', ' ');

            log.Add($"✅ {summary}");
            progress?.Invoke(log[^1]);
        }

        return log;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static string LoadEmbeddedResource(string logicalName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Try exact logical name first, then fall back to scanning all resource names.
        using var stream =
            assembly.GetManifestResourceStream(logicalName)
            ?? FindResourceStream(assembly, logicalName)
            ?? throw new InvalidOperationException(
                $"Embedded resource '{logicalName}' not found. " +
                $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static Stream? FindResourceStream(Assembly assembly, string logicalName)
    {
        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(logicalName, StringComparison.OrdinalIgnoreCase))
            {
                return assembly.GetManifestResourceStream(name);
            }
        }
        return null;
    }

    /// <summary>
    /// Splits a MySQL CLI script into individual executable statements.
    /// Handles DELIMITER changes so that stored procedure / trigger bodies
    /// are kept intact as single statements.
    /// </summary>
    private static List<string> SplitStatements(string script)
    {
        var statements = new List<string>();
        var current    = new StringBuilder();
        var delimiter  = ";";

        using var reader = new StringReader(script);
        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            var trimmedLine = line.Trim();

            // Strip single-line comments that aren't inside a procedure body.
            if (trimmedLine.StartsWith("--")) { continue; }

            // Handle DELIMITER directive — e.g.  DELIMITER $$  or  DELIMITER ;
            var delimMatch = Regex.Match(trimmedLine,
                @"^DELIMITER\s+(\S+)", RegexOptions.IgnoreCase);
            if (delimMatch.Success)
            {
                // Flush any pending statement before changing delimiter.
                var pending = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(pending))
                {
                    statements.Add(pending);
                    current.Clear();
                }
                delimiter = delimMatch.Groups[1].Value;
                continue;
            }

            current.AppendLine(line);

            // Check whether the accumulated buffer ends with the current delimiter.
            var buf = current.ToString();
            var idx = buf.LastIndexOf(delimiter, StringComparison.Ordinal);
            if (idx >= 0)
            {
                // Everything up to (and including) the delimiter is one statement.
                var stmt = buf[..idx].Trim();
                if (!string.IsNullOrWhiteSpace(stmt))
                {
                    statements.Add(stmt);
                }
                current.Clear();
                // Keep any trailing content after the delimiter on the next iteration.
                var tail = buf[(idx + delimiter.Length)..].Trim();
                if (!string.IsNullOrWhiteSpace(tail))
                {
                    current.AppendLine(tail);
                }
            }
        }

        // Flush anything remaining.
        var last = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(last))
        {
            statements.Add(last);
        }

        return statements;
    }
}

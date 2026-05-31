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
    private static readonly Regex DropIndexIfExistsRegex = new(
        @"^DROP\s+INDEX\s+IF\s+EXISTS\s+`?(?<index>[^`\s]+)`?\s+ON\s+`?(?<table>[^`\s;]+)`?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

            if (await TryExecuteDropIndexIfExistsAsync(connection, trimmed, cancellationToken))
            {
                var dropIndexSummary = trimmed.Length > 80
                    ? trimmed[..80].Replace('\n', ' ').Replace('\r', ' ') + "…"
                    : trimmed.Replace('\n', ' ').Replace('\r', ' ');

                log.Add($"✅ {dropIndexSummary}");
                progress?.Invoke(log[^1]);
                continue;
            }

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

    /// <summary>
    /// Parses the provided <paramref name="sqlContent"/> string, splits it into
    /// individual statements (handling DELIMITER directives), and executes each
    /// one against <paramref name="connection"/>.  Used for disk-based migration files.
    /// </summary>
    /// <remarks>
    /// Any <c>DEFINER = `user`@`host`</c> clause is stripped before execution so that
    /// objects are always created owned by the connected user.  This prevents MySQL
    /// Error 1227 ("you need SYSTEM_USER privilege") when re-running scripts that were
    /// originally authored by a <c>root</c> / SYSTEM_USER account.
    /// </remarks>
    /// <param name="connection">An open connection with the target database already selected.</param>
    /// <param name="sqlContent">Raw SQL content read from a migration file on disk.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static async Task RunFileScriptAsync(
        MySqlConnection connection,
        string sqlContent,
        CancellationToken cancellationToken = default)
    {
        var statements = SplitStatements(sqlContent);

        foreach (var statement in statements)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var trimmed = statement.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) { continue; }
            if (trimmed.StartsWith("USE ", StringComparison.OrdinalIgnoreCase)) { continue; }
            if (trimmed.StartsWith("CREATE DATABASE", StringComparison.OrdinalIgnoreCase)) { continue; }

            if (await TryExecuteDropIndexIfExistsAsync(connection, trimmed, cancellationToken))
            {
                continue;
            }

            // Strip DEFINER clauses so the object is owned by the running user,
            // preventing MySQL Error 1227 when the original author was root/SYSTEM_USER.
            var safeStatement = StripDefiner(trimmed);

            try
            {
                await using var cmd = connection.CreateCommand();
                cmd.CommandText = safeStatement;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (MySqlConnector.MySqlException ex)
            {
                var preview = safeStatement.Length > 120
                    ? safeStatement[..120].Replace('\n', ' ') + "…"
                    : safeStatement.Replace('\n', ' ');
                throw new InvalidOperationException(
                    $"MySQL error {ex.Number}: {ex.Message}\nStatement: {preview}", ex);
            }
        }
    }

    /// <summary>
    /// Removes a <c>DEFINER = `user`@`host`</c> (or <c>DEFINER = 'user'@'host'</c>)
    /// clause from a CREATE PROCEDURE / FUNCTION / TRIGGER / VIEW / EVENT statement.
    /// </summary>
    private static readonly Regex DefinerRegex = new(
        @"(?i)(CREATE\s+)DEFINER\s*=\s*(`[^`]*`|'[^']*')\s*@\s*(`[^`]*`|'[^']*')\s*",
        RegexOptions.Compiled);

    private static string StripDefiner(string sql) =>
        DefinerRegex.Replace(sql, "$1");

    /// <summary>
    /// Emulates MySQL 8's <c>DROP INDEX IF EXISTS</c> syntax on MySQL 5.7 by
    /// checking <c>information_schema.STATISTICS</c> and issuing
    /// <c>ALTER TABLE ... DROP INDEX ...</c> only when the index is present.
    /// </summary>
    private static async Task<bool> TryExecuteDropIndexIfExistsAsync(
        MySqlConnection connection,
        string statement,
        CancellationToken cancellationToken)
    {
        var match = DropIndexIfExistsRegex.Match(statement.Trim().TrimEnd(';'));
        if (!match.Success)
        {
            return false;
        }

        var indexName = match.Groups["index"].Value;
        var tableName = match.Groups["table"].Value;

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText =
            "SELECT COUNT(*) FROM information_schema.STATISTICS " +
            "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tableName AND INDEX_NAME = @indexName";
        existsCommand.Parameters.AddWithValue("@tableName", tableName);
        existsCommand.Parameters.AddWithValue("@indexName", indexName);

        var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync(cancellationToken)) > 0;
        if (!exists)
        {
            return true;
        }

        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText =
            $"ALTER TABLE `{EscapeIdentifier(tableName)}` DROP INDEX `{EscapeIdentifier(indexName)}`";
        await dropCommand.ExecuteNonQueryAsync(cancellationToken);

        return true;
    }

    private static string EscapeIdentifier(string identifier) =>
        identifier.Replace("`", "``", StringComparison.Ordinal);

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
        var current = new StringBuilder();
        var delimiter = ";";

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

using MTM_Waitlist_Server.Core.Models.Migration;
using System.Text;
using System.Text.RegularExpressions;

namespace MTM_Waitlist_Server.Core.Migration;

/// <summary>
/// Parses canonical table SQL files, compares normalized table schemas, and builds
/// deterministic restore-feasibility plans for the migration workflow.
/// </summary>
public static class TableSchemaToolkit
{
    private static readonly Regex CreateTableNameRegex = new(
        @"CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?`?(?<name>[^`\s(]+)`?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] ColumnDefinitionStopKeywords =
    [
        "not null",
        "null",
        "default",
        "auto_increment",
        "comment",
        "primary key",
        "unique",
        "constraint",
        "references",
        "on update",
        "on delete",
    ];

    private static readonly string[] DefaultValueStopKeywords =
    [
        "comment",
        "auto_increment",
        "primary key",
        "unique",
        "constraint",
        "references",
        "on update",
        "on delete",
    ];

    /// <summary>
    /// Parses a canonical CREATE TABLE script into a normalized schema model.
    /// </summary>
    public static TableSchemaDefinition ParseExpectedTableSchema(string sql, string sourcePath)
    {
        var createTableMatch = CreateTableNameRegex.Match(sql);
        if (!createTableMatch.Success)
        {
            throw new InvalidOperationException($"Could not locate a CREATE TABLE statement in {sourcePath}.");
        }

        string tableName = NormalizeIdentifier(createTableMatch.Groups["name"].Value);
        string body = ExtractCreateTableBody(sql);
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException($"Could not parse the CREATE TABLE body for {sourcePath}.");
        }

        List<TableColumnDefinition> columns = [];
        List<string> primaryKeyColumns = [];
        List<TableUniqueConstraintDefinition> uniqueConstraints = [];
        List<TableForeignKeyDefinition> foreignKeys = [];
        int ordinal = 0;

        foreach (string segment in SplitSqlSegments(body))
        {
            string normalizedSegment = NormalizeWhitespace(segment).Trim().TrimEnd(',');
            if (string.IsNullOrWhiteSpace(normalizedSegment))
            {
                continue;
            }

            if (normalizedSegment.StartsWith("`", StringComparison.Ordinal))
            {
                columns.Add(ParseColumn(normalizedSegment, ordinal++));
                continue;
            }

            if (normalizedSegment.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase))
            {
                primaryKeyColumns = ExtractNormalizedColumnList(normalizedSegment);
                continue;
            }

            if (IsUniqueConstraintSegment(normalizedSegment))
            {
                uniqueConstraints.Add(ParseUniqueConstraint(normalizedSegment));
                continue;
            }

            if (normalizedSegment.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
            {
                foreignKeys.Add(ParseForeignKey(normalizedSegment));
            }
        }

        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"No columns were parsed from {sourcePath}.");
        }

        return new TableSchemaDefinition(tableName, columns, primaryKeyColumns, uniqueConstraints, foreignKeys);
    }

    /// <summary>
    /// Compares expected and live table schemas and returns structured mismatch details.
    /// </summary>
    public static TableSchemaValidationResult CompareTableSchemas(
        string sourcePath,
        TableSchemaDefinition expected,
        TableSchemaDefinition live,
        IReadOnlyList<TableSchemaMismatch>? additionalMismatches = null)
    {
        List<TableSchemaMismatch> mismatches = [];

        var expectedColumns = expected.Columns.ToDictionary(column => column.Name, StringComparer.Ordinal);
        var liveColumns = live.Columns.ToDictionary(column => column.Name, StringComparer.Ordinal);

        foreach (var expectedColumn in expected.Columns)
        {
            if (!liveColumns.TryGetValue(expectedColumn.Name, out var liveColumn))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ColumnMissing,
                    TableMismatchSeverity.Repairable,
                    expectedColumn.Name,
                    "Column",
                    expectedColumn.Name,
                    null,
                    $"Column {expectedColumn.Name} is missing from the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
                continue;
            }

            if (!string.Equals(expectedColumn.NormalizedType, liveColumn.NormalizedType, StringComparison.Ordinal))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ColumnTypeMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedColumn.Name,
                    "Type",
                    expectedColumn.NormalizedType,
                    liveColumn.NormalizedType,
                    $"Column {expectedColumn.Name} has a different type in the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }

            if (expectedColumn.IsNullable != liveColumn.IsNullable)
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ColumnNullabilityMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedColumn.Name,
                    "IsNullable",
                    expectedColumn.IsNullable.ToString(),
                    liveColumn.IsNullable.ToString(),
                    $"Column {expectedColumn.Name} has different nullability in the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }

            if (!string.Equals(expectedColumn.NormalizedDefault, liveColumn.NormalizedDefault, StringComparison.Ordinal))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ColumnDefaultMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedColumn.Name,
                    "Default",
                    expectedColumn.NormalizedDefault,
                    liveColumn.NormalizedDefault,
                    $"Column {expectedColumn.Name} has a different default value in the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }

            if (!string.Equals(expectedColumn.NormalizedExtra, liveColumn.NormalizedExtra, StringComparison.Ordinal))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ColumnExtraMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedColumn.Name,
                    "Extra",
                    expectedColumn.NormalizedExtra,
                    liveColumn.NormalizedExtra,
                    $"Column {expectedColumn.Name} has different extra attributes in the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }
        }

        foreach (var liveColumn in live.Columns)
        {
            if (!expectedColumns.ContainsKey(liveColumn.Name))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ColumnUnexpected,
                    TableMismatchSeverity.Repairable,
                    liveColumn.Name,
                    "Column",
                    null,
                    liveColumn.Name,
                    $"Column {liveColumn.Name} exists in the live table but not in the canonical SQL file.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }
        }

        if (!expected.PrimaryKeyColumns.SequenceEqual(live.PrimaryKeyColumns, StringComparer.Ordinal))
        {
            mismatches.Add(CreateMismatch(
                TableMismatchKind.PrimaryKeyMismatch,
                TableMismatchSeverity.Repairable,
                expected.TableName,
                "PrimaryKey",
                string.Join(",", expected.PrimaryKeyColumns),
                string.Join(",", live.PrimaryKeyColumns),
                $"Primary key columns for {expected.TableName} do not match the canonical SQL file.",
                "Rebuild the table from the canonical SQL and restore compatible data."));
        }

        CompareUniqueConstraints(expected.UniqueConstraints, live.UniqueConstraints, mismatches);
        CompareForeignKeys(expected.ForeignKeys, live.ForeignKeys, mismatches);

        if (mismatches.Count > 0 && additionalMismatches is not null)
        {
            mismatches.AddRange(additionalMismatches);
        }

        TableValidationStatus status = mismatches.Count == 0 ? TableValidationStatus.Match : TableValidationStatus.Mismatch;
        bool canRepair = status == TableValidationStatus.Mismatch && mismatches.All(mismatch => mismatch.Severity != TableMismatchSeverity.Blocking);

        return new TableSchemaValidationResult(
            expected.TableName,
            sourcePath,
            status,
            mismatches,
            canRepair,
            BuildSummary(status, mismatches, canRepair));
    }

    /// <summary>
    /// Builds the restore plan that determines which backup columns can be restored safely.
    /// </summary>
    public static TableRestorePlan BuildRestorePlan(TableSchemaDefinition expected, TableSchemaDefinition backup)
    {
        HashSet<string> backupColumns = backup.Columns.Select(column => column.Name).ToHashSet(StringComparer.Ordinal);
        List<string> commonColumns = expected.Columns
            .Select(column => column.Name)
            .Where(backupColumns.Contains)
            .ToList();

        List<string> missingRequiredColumns = expected.Columns
            .Where(column => !backupColumns.Contains(column.Name))
            .Where(column => !column.IsNullable && string.IsNullOrEmpty(column.NormalizedDefault) && !string.Equals(column.NormalizedExtra, "auto_increment", StringComparison.Ordinal))
            .Select(column => column.Name)
            .ToList();

        return new TableRestorePlan(commonColumns, missingRequiredColumns);
    }

    /// <summary>
    /// Creates a structured unreadable-result object for parse or metadata failures.
    /// </summary>
    public static TableSchemaValidationResult CreateUnreadableResult(
        string tableName,
        string sourcePath,
        TableMismatchKind kind,
        string message)
    {
        var mismatch = CreateMismatch(
            kind,
            TableMismatchSeverity.Blocking,
            tableName,
            "Schema",
            null,
            null,
            message,
            "Review the canonical SQL file and live metadata before applying updates.");

        return new TableSchemaValidationResult(
            tableName,
            sourcePath,
            TableValidationStatus.Unreadable,
            [mismatch],
            false,
            message);
    }

    /// <summary>
    /// Normalizes a MySQL column type from live metadata or canonical SQL into the
    /// deterministic representation used by schema validation.
    /// </summary>
    public static string NormalizeColumnTypeForComparison(string input) =>
        NormalizeColumnType(input);

    /// <summary>
    /// Normalizes a MySQL default literal from live metadata or canonical SQL into the
    /// deterministic representation used by schema validation.
    /// </summary>
    public static string? NormalizeDefaultLiteralForComparison(string input, bool isNullable) =>
        NormalizeDefaultLiteral(input, isNullable);

    /// <summary>
    /// Normalizes a MySQL column extra value from live metadata or canonical SQL into the
    /// deterministic representation used by schema validation.
    /// </summary>
    public static string NormalizeExtraForComparison(string input) =>
        NormalizeWhitespace(input).ToLowerInvariant();

    /// <summary>
    /// Creates a structured missing-table result for a canonical table definition.
    /// </summary>
    public static TableSchemaValidationResult CreateMissingResult(string tableName, string sourcePath)
    {
        return new TableSchemaValidationResult(
            tableName,
            sourcePath,
            TableValidationStatus.Missing,
            [CreateMismatch(
                TableMismatchKind.TableMissing,
                TableMismatchSeverity.Repairable,
                tableName,
                "Table",
                tableName,
                null,
                $"Table {tableName} is missing from the configured database.",
                "Create the table from the canonical SQL file.")],
            true,
            "Table missing - will be created from canonical SQL.");
    }

    private static void CompareUniqueConstraints(
        IReadOnlyList<TableUniqueConstraintDefinition> expected,
        IReadOnlyList<TableUniqueConstraintDefinition> live,
        List<TableSchemaMismatch> mismatches)
    {
        var expectedByColumns = expected.ToDictionary(constraint => string.Join(",", constraint.Columns), StringComparer.Ordinal);
        var liveByColumns = live.ToDictionary(constraint => string.Join(",", constraint.Columns), StringComparer.Ordinal);

        foreach (var expectedConstraint in expected)
        {
            string key = string.Join(",", expectedConstraint.Columns);
            if (!liveByColumns.ContainsKey(key))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.UniqueConstraintMissing,
                    TableMismatchSeverity.Repairable,
                    expectedConstraint.Name,
                    "Columns",
                    key,
                    null,
                    $"Unique constraint {expectedConstraint.Name} is missing from the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }
        }

        foreach (var liveConstraint in live)
        {
            string key = string.Join(",", liveConstraint.Columns);
            if (!expectedByColumns.ContainsKey(key))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.UniqueConstraintUnexpected,
                    TableMismatchSeverity.Repairable,
                    liveConstraint.Name,
                    "Columns",
                    null,
                    key,
                    $"Unique constraint {liveConstraint.Name} exists in the live table but not in the canonical SQL file.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }
        }
    }

    private static void CompareForeignKeys(
        IReadOnlyList<TableForeignKeyDefinition> expected,
        IReadOnlyList<TableForeignKeyDefinition> live,
        List<TableSchemaMismatch> mismatches)
    {
        var expectedByColumns = expected.ToDictionary(foreignKey => string.Join(",", foreignKey.Columns), StringComparer.Ordinal);
        var liveByColumns = live.ToDictionary(foreignKey => string.Join(",", foreignKey.Columns), StringComparer.Ordinal);

        foreach (var expectedForeignKey in expected)
        {
            string key = string.Join(",", expectedForeignKey.Columns);
            if (!liveByColumns.TryGetValue(key, out var liveForeignKey))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ForeignKeyMissing,
                    TableMismatchSeverity.Repairable,
                    expectedForeignKey.Name,
                    "Columns",
                    key,
                    null,
                    $"Foreign key {expectedForeignKey.Name} is missing from the live table.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
                continue;
            }

            if (!string.Equals(expectedForeignKey.ReferencedTable, liveForeignKey.ReferencedTable, StringComparison.Ordinal))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ForeignKeyReferencedTableMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedForeignKey.Name,
                    "ReferencedTable",
                    expectedForeignKey.ReferencedTable,
                    liveForeignKey.ReferencedTable,
                    $"Foreign key {expectedForeignKey.Name} points to a different table in live metadata.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }

            if (!expectedForeignKey.ReferencedColumns.SequenceEqual(liveForeignKey.ReferencedColumns, StringComparer.Ordinal))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ForeignKeyReferencedColumnsMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedForeignKey.Name,
                    "ReferencedColumns",
                    string.Join(",", expectedForeignKey.ReferencedColumns),
                    string.Join(",", liveForeignKey.ReferencedColumns),
                    $"Foreign key {expectedForeignKey.Name} points to different referenced columns in live metadata.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }

            if (!string.Equals(expectedForeignKey.DeleteRule, liveForeignKey.DeleteRule, StringComparison.Ordinal))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ForeignKeyDeleteRuleMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedForeignKey.Name,
                    "DeleteRule",
                    expectedForeignKey.DeleteRule,
                    liveForeignKey.DeleteRule,
                    $"Foreign key {expectedForeignKey.Name} has a different ON DELETE rule in live metadata.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }

            if (!string.Equals(expectedForeignKey.UpdateRule, liveForeignKey.UpdateRule, StringComparison.Ordinal))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ForeignKeyUpdateRuleMismatch,
                    TableMismatchSeverity.Repairable,
                    expectedForeignKey.Name,
                    "UpdateRule",
                    expectedForeignKey.UpdateRule,
                    liveForeignKey.UpdateRule,
                    $"Foreign key {expectedForeignKey.Name} has a different ON UPDATE rule in live metadata.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }
        }

        foreach (var liveForeignKey in live)
        {
            string key = string.Join(",", liveForeignKey.Columns);
            if (!expectedByColumns.ContainsKey(key))
            {
                mismatches.Add(CreateMismatch(
                    TableMismatchKind.ForeignKeyUnexpected,
                    TableMismatchSeverity.Repairable,
                    liveForeignKey.Name,
                    "Columns",
                    null,
                    key,
                    $"Foreign key {liveForeignKey.Name} exists in the live table but not in the canonical SQL file.",
                    "Rebuild the table from the canonical SQL and restore compatible data."));
            }
        }
    }

    private static TableColumnDefinition ParseColumn(string segment, int ordinal)
    {
        int secondTickIndex = segment.IndexOf('`', 1);
        if (secondTickIndex <= 0)
        {
            throw new InvalidOperationException($"Could not parse a column name from segment: {segment}");
        }

        string name = NormalizeIdentifier(segment[1..secondTickIndex]);
        string definition = segment[(secondTickIndex + 1)..].Trim();
        definition = Regex.Replace(definition, @"COMMENT\s+'(?:''|[^'])*'", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        string typeSegment = ExtractTopLevelSegment(definition, ColumnDefinitionStopKeywords);
        bool isNullable = !Regex.IsMatch(definition, @"\bNOT\s+NULL\b", RegexOptions.IgnoreCase);
        string? defaultValue = ExtractDefaultValue(definition, isNullable);
        string extra = ExtractExtra(definition);

        return new TableColumnDefinition(
            name,
            NormalizeColumnType(typeSegment),
            isNullable,
            defaultValue,
            extra,
            ordinal);
    }

    private static TableUniqueConstraintDefinition ParseUniqueConstraint(string segment)
    {
        string name = ExtractConstraintName(segment, "unique");
        List<string> columns = ExtractNormalizedColumnList(segment);
        return new TableUniqueConstraintDefinition(name, columns);
    }

    private static TableForeignKeyDefinition ParseForeignKey(string segment)
    {
        string name = ExtractConstraintName(segment, "foreignkey");

        var localMatch = Regex.Match(segment, @"FOREIGN\s+KEY\s*\((?<columns>[^)]+)\)", RegexOptions.IgnoreCase);
        if (!localMatch.Success)
        {
            throw new InvalidOperationException($"Could not parse foreign key columns from segment: {segment}");
        }

        var referenceMatch = Regex.Match(segment, @"REFERENCES\s+`?(?<table>[^`\s(]+)`?\s*\((?<columns>[^)]+)\)", RegexOptions.IgnoreCase);
        if (!referenceMatch.Success)
        {
            throw new InvalidOperationException($"Could not parse foreign key reference from segment: {segment}");
        }

        string deleteRule = ExtractReferentialRule(segment, "delete");
        string updateRule = ExtractReferentialRule(segment, "update");

        return new TableForeignKeyDefinition(
            name,
            ExtractNormalizedColumnList(localMatch.Groups["columns"].Value),
            NormalizeIdentifier(referenceMatch.Groups["table"].Value),
            ExtractNormalizedColumnList(referenceMatch.Groups["columns"].Value),
            deleteRule,
            updateRule);
    }

    private static string ExtractConstraintName(string segment, string fallbackPrefix)
    {
        var match = Regex.Match(segment, @"CONSTRAINT\s+`?(?<name>[^`\s]+)`?", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return NormalizeIdentifier(match.Groups["name"].Value);
        }

        var keyMatch = Regex.Match(segment, @"(?:UNIQUE\s+(?:KEY|INDEX)|KEY)\s+`?(?<name>[^`\s(]+)`?", RegexOptions.IgnoreCase);
        if (keyMatch.Success)
        {
            return NormalizeIdentifier(keyMatch.Groups["name"].Value);
        }

        return $"{fallbackPrefix}_{ComputeStableName(segment)}";
    }

    private static List<string> ExtractNormalizedColumnList(string segment)
    {
        int openParenIndex = segment.IndexOf('(');
        int closeParenIndex = FindMatchingCloseParenthesis(segment, openParenIndex);
        string columnSegment = openParenIndex >= 0 && closeParenIndex > openParenIndex
            ? segment[(openParenIndex + 1)..closeParenIndex]
            : segment;

        return Regex.Matches(columnSegment, @"`(?<name>[^`]+)`")
            .Select(match => NormalizeIdentifier(match.Groups["name"].Value))
            .ToList();
    }

    private static string ExtractReferentialRule(string segment, string ruleKind)
    {
        var match = Regex.Match(
            segment,
            $@"ON\s+{ruleKind}\s+(?<rule>CASCADE|RESTRICT|SET\s+NULL|NO\s+ACTION)",
            RegexOptions.IgnoreCase);

        return match.Success
            ? NormalizeReferentialAction(match.Groups["rule"].Value)
            : "restrict";
    }

    private static string ExtractTopLevelSegment(string definition, IReadOnlyList<string> stopKeywords)
    {
        StringBuilder builder = new();
        int depth = 0;
        bool inSingleQuote = false;

        for (int index = 0; index < definition.Length; index++)
        {
            char current = definition[index];
            bool escaped = index > 0 && definition[index - 1] == '\\';

            if (current == '\'' && !escaped)
            {
                inSingleQuote = !inSingleQuote;
            }

            if (!inSingleQuote)
            {
                if (current == '(')
                {
                    depth++;
                }
                else if (current == ')')
                {
                    depth--;
                }
                else if (depth == 0 && char.IsWhiteSpace(current))
                {
                    string remainder = definition[index..].TrimStart();
                    if (StartsWithKeyword(remainder, stopKeywords))
                    {
                        break;
                    }
                }
            }

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }

    private static bool StartsWithKeyword(string remainder, IReadOnlyList<string> keywords)
    {
        foreach (string keyword in keywords)
        {
            if (remainder.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string? ExtractDefaultValue(string definition, bool isNullable)
    {
        int defaultIndex = IndexOfKeywordOutsideQuotes(definition, "default");
        if (defaultIndex < 0)
        {
            return null;
        }

        string remainder = definition[(defaultIndex + "default".Length)..].TrimStart();
        string defaultSegment = ExtractTopLevelSegment(remainder, DefaultValueStopKeywords);
        return NormalizeDefaultLiteral(defaultSegment, isNullable);
    }

    private static int IndexOfKeywordOutsideQuotes(string input, string keyword)
    {
        bool inSingleQuote = false;
        int depth = 0;

        for (int index = 0; index < input.Length; index++)
        {
            char current = input[index];
            bool escaped = index > 0 && input[index - 1] == '\\';

            if (current == '\'' && !escaped)
            {
                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (inSingleQuote)
            {
                continue;
            }

            if (current == '(')
            {
                depth++;
                continue;
            }

            if (current == ')')
            {
                depth--;
                continue;
            }

            if (depth == 0 && input[index..].StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static string ExtractExtra(string definition)
    {
        List<string> extraParts = [];

        if (Regex.IsMatch(definition, @"\bAUTO_INCREMENT\b", RegexOptions.IgnoreCase))
        {
            extraParts.Add("auto_increment");
        }

        var onUpdateMatch = Regex.Match(definition, @"ON\s+UPDATE\s+(?<value>CURRENT_TIMESTAMP(?:\(\))?)", RegexOptions.IgnoreCase);
        if (onUpdateMatch.Success)
        {
            extraParts.Add($"on update {NormalizeDefaultLiteral(onUpdateMatch.Groups["value"].Value, false)}");
        }

        return string.Join(' ', extraParts);
    }

    private static string NormalizeColumnType(string input)
    {
        string normalized = NormalizeWhitespace(input).ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\b(int|bigint|smallint|mediumint)\(\d+\)", "$1", RegexOptions.IgnoreCase);
        normalized = Regex.Replace(normalized, @"\bcurrent_timestamp\(\)", "current_timestamp", RegexOptions.IgnoreCase);

        if (normalized.StartsWith("enum", StringComparison.Ordinal))
        {
            return NormalizeEnumType(normalized);
        }

        normalized = Regex.Replace(normalized, @"\s*,\s*", ",");
        normalized = Regex.Replace(normalized, @"\s*\)", ")");
        normalized = Regex.Replace(normalized, @"\(\s*", "(");
        return normalized;
    }

    private static string NormalizeEnumType(string input)
    {
        int openParenIndex = input.IndexOf('(');
        int closeParenIndex = input.LastIndexOf(')');
        if (openParenIndex < 0 || closeParenIndex <= openParenIndex)
        {
            return input;
        }

        string valuesSegment = input[(openParenIndex + 1)..closeParenIndex];
        List<string> values = SplitSqlSegments(valuesSegment)
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Select(value => NormalizeDefaultLiteral(value, false) ?? string.Empty)
            .ToList();

        return $"enum('{string.Join("','", values)}')";
    }

    private static string? NormalizeDefaultLiteral(string input, bool isNullable)
    {
        string normalized = NormalizeWhitespace(input).Trim().TrimEnd(',');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (string.Equals(normalized, "null", StringComparison.OrdinalIgnoreCase))
        {
            return isNullable ? null : "null";
        }

        if (normalized.StartsWith("'", StringComparison.Ordinal) && normalized.EndsWith("'", StringComparison.Ordinal) && normalized.Length >= 2)
        {
            normalized = normalized[1..^1];
        }

        normalized = normalized.Replace("''", "'", StringComparison.Ordinal);
        normalized = Regex.Replace(normalized, @"\bcurrent_timestamp\(\)", "current_timestamp", RegexOptions.IgnoreCase);
        return normalized.ToLowerInvariant();
    }

    private static bool IsUniqueConstraintSegment(string segment) =>
        segment.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
        segment.StartsWith("UNIQUE KEY", StringComparison.OrdinalIgnoreCase) ||
        segment.StartsWith("UNIQUE INDEX", StringComparison.OrdinalIgnoreCase);

    private static string ExtractCreateTableBody(string sql)
    {
        var createTableMatch = CreateTableNameRegex.Match(sql);
        int searchStartIndex = createTableMatch.Success
            ? createTableMatch.Index + createTableMatch.Length
            : 0;

        int openParenIndex = sql.IndexOf('(', searchStartIndex);
        int closeParenIndex = FindMatchingCloseParenthesis(sql, openParenIndex);
        if (openParenIndex < 0 || closeParenIndex <= openParenIndex)
        {
            return string.Empty;
        }

        return sql[(openParenIndex + 1)..closeParenIndex];
    }

    private static int FindMatchingCloseParenthesis(string sql, int openParenIndex)
    {
        if (openParenIndex < 0)
        {
            return -1;
        }

        int depth = 0;
        bool inSingleQuote = false;
        bool inDoubleQuote = false;

        for (int index = openParenIndex; index < sql.Length; index++)
        {
            char current = sql[index];
            bool escaped = index > 0 && sql[index - 1] == '\\';

            if (current == '\'' && !inDoubleQuote && !escaped)
            {
                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (current == '"' && !inSingleQuote && !escaped)
            {
                inDoubleQuote = !inDoubleQuote;
                continue;
            }

            if (inSingleQuote || inDoubleQuote)
            {
                continue;
            }

            if (current == '(')
            {
                depth++;
            }
            else if (current == ')')
            {
                depth--;
                if (depth == 0)
                {
                    return index;
                }
            }
        }

        return -1;
    }

    private static List<string> SplitSqlSegments(string sql)
    {
        List<string> segments = [];
        StringBuilder current = new();
        int parenthesisDepth = 0;
        bool inString = false;

        for (int index = 0; index < sql.Length; index++)
        {
            char currentCharacter = sql[index];
            if (currentCharacter == '\'' && (index == 0 || sql[index - 1] != '\\'))
            {
                inString = !inString;
            }

            if (!inString)
            {
                if (currentCharacter == '(')
                {
                    parenthesisDepth++;
                }
                else if (currentCharacter == ')')
                {
                    parenthesisDepth--;
                }
                else if (currentCharacter == ',' && parenthesisDepth == 0)
                {
                    segments.Add(current.ToString());
                    current.Clear();
                    continue;
                }
            }

            current.Append(currentCharacter);
        }

        if (current.Length > 0)
        {
            segments.Add(current.ToString());
        }

        return segments;
    }

    private static string NormalizeIdentifier(string identifier) =>
        identifier.Replace("`", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

    private static string NormalizeReferentialAction(string action) =>
        NormalizeWhitespace(action).Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();

    private static TableSchemaMismatch CreateMismatch(
        TableMismatchKind kind,
        TableMismatchSeverity severity,
        string objectName,
        string propertyName,
        string? expectedValue,
        string? actualValue,
        string message,
        string recommendedAction) =>
        new(kind, severity, objectName, propertyName, expectedValue, actualValue, message, recommendedAction);

    private static string ComputeStableName(string segment)
    {
        int hash = StringComparer.Ordinal.GetHashCode(NormalizeWhitespace(segment).ToLowerInvariant());
        return Math.Abs(hash).ToString("x8", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string BuildSummary(TableValidationStatus status, IReadOnlyList<TableSchemaMismatch> mismatches, bool canRepair)
    {
        return status switch
        {
            TableValidationStatus.Match => "Table matches canonical SQL.",
            TableValidationStatus.Missing => "Table missing - will be created from canonical SQL.",
            TableValidationStatus.Unreadable => mismatches.FirstOrDefault()?.Message ?? "Table definition could not be validated.",
            _ => canRepair
                ? $"Table mismatch - safe repair available ({mismatches.Count} issue(s))."
                : $"Table mismatch - manual action required ({mismatches.Count} issue(s)).",
        };
    }
}

/// <summary>
/// Normalized representation of a table schema used for deterministic comparison.
/// </summary>
public sealed record TableSchemaDefinition(
    string TableName,
    IReadOnlyList<TableColumnDefinition> Columns,
    IReadOnlyList<string> PrimaryKeyColumns,
    IReadOnlyList<TableUniqueConstraintDefinition> UniqueConstraints,
    IReadOnlyList<TableForeignKeyDefinition> ForeignKeys);

/// <summary>
/// Normalized representation of a table column.
/// </summary>
public sealed record TableColumnDefinition(
    string Name,
    string NormalizedType,
    bool IsNullable,
    string? NormalizedDefault,
    string NormalizedExtra,
    int Ordinal);

/// <summary>
/// Normalized representation of a unique constraint.
/// </summary>
public sealed record TableUniqueConstraintDefinition(string Name, IReadOnlyList<string> Columns);

/// <summary>
/// Normalized representation of a foreign key.
/// </summary>
public sealed record TableForeignKeyDefinition(
    string Name,
    IReadOnlyList<string> Columns,
    string ReferencedTable,
    IReadOnlyList<string> ReferencedColumns,
    string DeleteRule,
    string UpdateRule);

/// <summary>
/// Describes which backup columns can be restored into a rebuilt table safely.
/// </summary>
public sealed record TableRestorePlan(
    IReadOnlyList<string> CommonColumns,
    IReadOnlyList<string> MissingRequiredColumns);
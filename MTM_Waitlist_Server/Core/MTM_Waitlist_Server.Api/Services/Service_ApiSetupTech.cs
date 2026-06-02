using System.Globalization;
using MySqlConnector;
using MTM_Waitlist_Server.Api.Models;
using MTM_Waitlist_Server.Api.Models.SetupTech;
using MTM_Waitlist_Server.Core.Interfaces.Settings;

namespace MTM_Waitlist_Server.Api.Services;

/// <summary>
/// Implements the REST API SetupTech flows used by the MAUI client.
/// Reads and writes SetupTech data through the MySQL stored procedure set and cache tables.
/// </summary>
public sealed class Service_ApiSetupTech
{
    private readonly IService_SettingsStore _settingsStore;

    /// <summary>
    /// Initialises a new instance with access to persisted server database settings.
    /// </summary>
    public Service_ApiSetupTech(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <summary>
    /// Returns the current active job row for a workstation together with cached subordinate parts and dunnage assignments.
    /// </summary>
    public async Task<Model_Dao_Result<Model_SetupTech_ActiveJob>> GetActiveJobAsync(string workcenterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workcenterId))
        {
            return Model_Dao_Result<Model_SetupTech_ActiveJob>.Failure("A workcenter ID is required.");
        }

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "CALL usp_SetupTech_GetActiveJob(@p_WorkcenterId)";
            command.Parameters.AddWithValue("@p_WorkcenterId", workcenterId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return Model_Dao_Result<Model_SetupTech_ActiveJob>.Failure($"No active job was found for workcenter '{workcenterId}'.");
            }

            var activeJob = new Model_SetupTech_ActiveJob
            {
                Id = Convert.ToInt32(reader["Id"], CultureInfo.InvariantCulture),
                WorkcenterId = reader["WorkcenterId"]?.ToString() ?? string.Empty,
                WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                SequenceNo = Convert.ToInt32(reader["SequenceNo"], CultureInfo.InvariantCulture),
                PartId = reader["PartId"]?.ToString() ?? string.Empty,
                PartType = reader["PartType"]?.ToString() ?? string.Empty,
                SetupTechUserId = Convert.ToInt32(reader["SetupTechUserId"], CultureInfo.InvariantCulture),
                ActiveSince = Convert.ToDateTime(reader["ActiveSince"], CultureInfo.InvariantCulture),
            };

            activeJob.SubordinateParts = await LoadCachedSubordinatePartsAsync(connection, activeJob.WorkOrderId, activeJob.SequenceNo, cancellationToken);
            activeJob.DunnageAssignments = await LoadDunnageAssignmentsAsync(connection, activeJob.WorkOrderId, activeJob.SequenceNo, cancellationToken);

            return Model_Dao_Result<Model_SetupTech_ActiveJob>.Success(activeJob);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<Model_SetupTech_ActiveJob>.Failure($"SetupTech data is unavailable: {ex.Message}");
        }
    }

    /// <summary>
    /// Archives the existing active job for the workstation, inserts the new one, and refreshes the subordinate part cache.
    /// </summary>
    public async Task<Model_Dao_Result> SetActiveJobAsync(Model_SetupTech_ActiveJob activeJob, CancellationToken cancellationToken = default)
    {
        if (!IsValidActiveJob(activeJob))
        {
            return Model_Dao_Result.Failure("A valid active job payload is required.");
        }

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);

            await using (var command = connection.CreateCommand())
            {
                command.CommandText = "CALL usp_SetupTech_SetActiveJob(@p_WorkcenterId, @p_WorkOrderId, @p_SequenceNo, @p_PartId, @p_PartType, @p_SetupTechUserId, @p_ActiveSince)";
                command.Parameters.AddWithValue("@p_WorkcenterId", activeJob.WorkcenterId);
                command.Parameters.AddWithValue("@p_WorkOrderId", activeJob.WorkOrderId);
                command.Parameters.AddWithValue("@p_SequenceNo", activeJob.SequenceNo);
                command.Parameters.AddWithValue("@p_PartId", activeJob.PartId);
                command.Parameters.AddWithValue("@p_PartType", activeJob.PartType);
                command.Parameters.AddWithValue("@p_SetupTechUserId", activeJob.SetupTechUserId);
                command.Parameters.AddWithValue("@p_ActiveSince", activeJob.ActiveSince == default ? DBNull.Value : activeJob.ActiveSince);
                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await ClearCachedSubordinatePartsAsync(connection, activeJob.WorkOrderId, activeJob.SequenceNo, cancellationToken);
            foreach (var subordinatePart in activeJob.SubordinateParts)
            {
                await UpsertCachedSubordinatePartAsync(connection, activeJob.WorkOrderId, activeJob.SequenceNo, subordinatePart, cancellationToken);
            }

            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"The active job could not be saved: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the cached dunnage assignment rows for a work order and sequence.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>> GetDunnageAssignmentAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
        {
            return Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Failure("A work order ID is required.");
        }

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);
            var assignments = await LoadDunnageAssignmentsAsync(connection, workOrderId, sequenceNo, cancellationToken);
            return Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Success(assignments);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_SetupTech_DunnageAssignment>>.Failure($"Dunnage assignment data is unavailable: {ex.Message}");
        }
    }

    /// <summary>
    /// Inserts or updates one dunnage assignment row for a work order and sequence.
    /// </summary>
    public async Task<Model_Dao_Result> UpsertDunnageAssignmentAsync(Model_SetupTech_DunnageAssignment assignment, CancellationToken cancellationToken = default)
    {
        if (!IsValidAssignment(assignment))
        {
            return Model_Dao_Result.Failure("A valid dunnage assignment payload is required.");
        }

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "CALL usp_SetupTech_UpsertDunnageAssignment(@p_WorkOrderId, @p_SequenceNo, @p_DunnagePartId, @p_DunnagePartName, @p_DunnageTypeId, @p_DunnageTypeName, @p_LastModifiedByUserId)";
            command.Parameters.AddWithValue("@p_WorkOrderId", assignment.WorkOrderId);
            command.Parameters.AddWithValue("@p_SequenceNo", assignment.SequenceNo);
            command.Parameters.AddWithValue("@p_DunnagePartId", assignment.DunnagePartId);
            command.Parameters.AddWithValue("@p_DunnagePartName", assignment.DunnagePartName);
            command.Parameters.AddWithValue("@p_DunnageTypeId", assignment.DunnageTypeId);
            command.Parameters.AddWithValue("@p_DunnageTypeName", assignment.DunnageTypeName);
            command.Parameters.AddWithValue("@p_LastModifiedByUserId", assignment.LastModifiedByUserId);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"The dunnage assignment could not be saved: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes one dunnage assignment row by identifier.
    /// </summary>
    public async Task<Model_Dao_Result> DeleteDunnageAssignmentAsync(int assignmentId, CancellationToken cancellationToken = default)
    {
        if (assignmentId <= 0)
        {
            return Model_Dao_Result.Failure("A valid dunnage assignment ID is required.");
        }

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "CALL usp_SetupTech_DeleteDunnageAssignment(@p_Id)";
            command.Parameters.AddWithValue("@p_Id", assignmentId);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return Model_Dao_Result.Success();
        }
        catch (Exception ex)
        {
            return Model_Dao_Result.Failure($"The dunnage assignment could not be deleted: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the enabled dunnage type categories for the SetupTech UI.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>> GetEnabledDunnageTypesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "CALL usp_SetupTech_GetEnabledDunnageTypes()";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var types = new List<Model_SetupTech_DunnageTypeConfig>();
            while (await reader.ReadAsync(cancellationToken))
            {
                types.Add(new Model_SetupTech_DunnageTypeConfig
                {
                    DunnageTypeId = Convert.ToInt32(reader["DunnageTypeId"], CultureInfo.InvariantCulture),
                    DunnageTypeName = reader["DunnageTypeName"]?.ToString() ?? string.Empty,
                    IsEnabled = Convert.ToBoolean(reader["IsEnabled"], CultureInfo.InvariantCulture),
                    DisplayOrder = Convert.ToInt32(reader["DisplayOrder"], CultureInfo.InvariantCulture),
                });
            }

            return Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Success(types);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_SetupTech_DunnageTypeConfig>>.Failure($"Dunnage type configuration is unavailable: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns archived job history for a workstation.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_SetupTech_ActiveJob>>> GetJobHistoryAsync(string workcenterId, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workcenterId))
        {
            return Model_Dao_Result<List<Model_SetupTech_ActiveJob>>.Failure("A workcenter ID is required.");
        }

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "CALL usp_SetupTech_GetJobHistory(@p_WorkcenterId, @p_PageSize)";
            command.Parameters.AddWithValue("@p_WorkcenterId", workcenterId);
            command.Parameters.AddWithValue("@p_PageSize", pageSize <= 0 ? 50 : pageSize);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<Model_SetupTech_ActiveJob>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new Model_SetupTech_ActiveJob
                {
                    Id = Convert.ToInt32(reader["Id"], CultureInfo.InvariantCulture),
                    WorkcenterId = reader["WorkcenterId"]?.ToString() ?? string.Empty,
                    WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                    SequenceNo = Convert.ToInt32(reader["SequenceNo"], CultureInfo.InvariantCulture),
                    PartId = reader["PartId"]?.ToString() ?? string.Empty,
                    PartType = reader["PartType"]?.ToString() ?? string.Empty,
                    SetupTechUserId = Convert.ToInt32(reader["SetupTechUserId"], CultureInfo.InvariantCulture),
                    ActiveSince = Convert.ToDateTime(reader["ActiveFrom"], CultureInfo.InvariantCulture),
                    SubordinateParts = [],
                    DunnageAssignments = [],
                });
            }

            return Model_Dao_Result<List<Model_SetupTech_ActiveJob>>.Success(items);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_SetupTech_ActiveJob>>.Failure($"SetupTech job history is unavailable: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns the cross-database dunnage catalog from the receiving application database.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_DunnagePart>>> GetDunnageCatalogAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                p.id AS DunnagePartId,
                COALESCE(p.part_id, '') AS DunnagePartName,
                t.id AS DunnageTypeId,
                COALESCE(t.type_name, '') AS DunnageTypeName
            FROM mtm_receiving_application.dunnage_parts p
            INNER JOIN mtm_receiving_application.dunnage_types t
                ON t.id = p.type_id
            ORDER BY t.type_name, p.part_id, p.id;
            """;

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<Model_DunnagePart>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new Model_DunnagePart
                {
                    DunnagePartId = Convert.ToInt32(reader["DunnagePartId"], CultureInfo.InvariantCulture),
                    DunnagePartName = reader["DunnagePartName"]?.ToString() ?? string.Empty,
                    DunnageTypeId = Convert.ToInt32(reader["DunnageTypeId"], CultureInfo.InvariantCulture),
                    DunnageTypeName = reader["DunnageTypeName"]?.ToString() ?? string.Empty,
                });
            }

            return Model_Dao_Result<List<Model_DunnagePart>>.Success(items);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_DunnagePart>>.Failure($"The dunnage catalog is unavailable: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns cached subordinate parts for a work order and sequence pair.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_SetupTech_SubordinatePart>>> GetCachedSubordinatePartsAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
        {
            return Model_Dao_Result<List<Model_SetupTech_SubordinatePart>>.Failure("A work order ID is required.");
        }

        try
        {
            await using var connection = OpenAppConnection();
            await connection.OpenAsync(cancellationToken);
            var parts = await LoadCachedSubordinatePartsAsync(connection, workOrderId, sequenceNo, cancellationToken);
            return Model_Dao_Result<List<Model_SetupTech_SubordinatePart>>.Success(parts);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<Model_SetupTech_SubordinatePart>>.Failure($"Cached subordinate parts are unavailable: {ex.Message}");
        }
    }

    private MySqlConnection OpenAppConnection()
    {
        var database = _settingsStore.Get().Database;
        var builder = new MySqlConnectionStringBuilder
        {
            Server = database.Host,
            Port = (uint)database.Port,
            Database = database.DatabaseName,
            UserID = database.AppUsername,
            Password = database.AppPassword,
            ConnectionTimeout = (uint)database.ConnectionTimeout,
            DefaultCommandTimeout = (uint)database.CommandTimeout,
            AllowPublicKeyRetrieval = true,
            SslMode = MySqlSslMode.Preferred,
        };

        return new MySqlConnection(builder.ConnectionString);
    }

    private static bool IsValidActiveJob(Model_SetupTech_ActiveJob? activeJob)
    {
        return activeJob is not null
            && !string.IsNullOrWhiteSpace(activeJob.WorkcenterId)
            && !string.IsNullOrWhiteSpace(activeJob.WorkOrderId)
            && !string.IsNullOrWhiteSpace(activeJob.PartId)
            && activeJob.SequenceNo > 0
            && activeJob.SetupTechUserId > 0;
    }

    private static bool IsValidAssignment(Model_SetupTech_DunnageAssignment? assignment)
    {
        return assignment is not null
            && !string.IsNullOrWhiteSpace(assignment.WorkOrderId)
            && !string.IsNullOrWhiteSpace(assignment.DunnagePartName)
            && !string.IsNullOrWhiteSpace(assignment.DunnageTypeName)
            && assignment.SequenceNo > 0
            && assignment.DunnagePartId > 0
            && assignment.DunnageTypeId > 0
            && assignment.LastModifiedByUserId > 0;
    }

    private static async Task<List<Model_SetupTech_DunnageAssignment>> LoadDunnageAssignmentsAsync(MySqlConnection connection, string workOrderId, int sequenceNo, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_SetupTech_GetDunnageAssignment(@p_WorkOrderId, @p_SequenceNo)";
        command.Parameters.AddWithValue("@p_WorkOrderId", workOrderId);
        command.Parameters.AddWithValue("@p_SequenceNo", sequenceNo);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var assignments = new List<Model_SetupTech_DunnageAssignment>();
        while (await reader.ReadAsync(cancellationToken))
        {
            assignments.Add(new Model_SetupTech_DunnageAssignment
            {
                AssignmentId = Convert.ToInt32(reader["Id"], CultureInfo.InvariantCulture),
                WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                SequenceNo = Convert.ToInt32(reader["SequenceNo"], CultureInfo.InvariantCulture),
                DunnagePartId = Convert.ToInt32(reader["DunnagePartId"], CultureInfo.InvariantCulture),
                DunnagePartName = reader["DunnagePartName"]?.ToString() ?? string.Empty,
                DunnageTypeId = Convert.ToInt32(reader["DunnageTypeId"], CultureInfo.InvariantCulture),
                DunnageTypeName = reader["DunnageTypeName"]?.ToString() ?? string.Empty,
                LastModifiedByUserId = Convert.ToInt32(reader["LastModifiedByUserId"], CultureInfo.InvariantCulture),
            });
        }

        return assignments;
    }

    private static async Task<List<Model_SetupTech_SubordinatePart>> LoadCachedSubordinatePartsAsync(MySqlConnection connection, string workOrderId, int sequenceNo, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT WorkOrderId, SequenceNo, SubPartId, SubPartDesc, RequiredQty, QtyOnHand FROM WorkOrderSubordinateParts WHERE WorkOrderId = @p_WorkOrderId AND SequenceNo = @p_SequenceNo ORDER BY SubPartId;";
        command.Parameters.AddWithValue("@p_WorkOrderId", workOrderId);
        command.Parameters.AddWithValue("@p_SequenceNo", sequenceNo);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var parts = new List<Model_SetupTech_SubordinatePart>();
        while (await reader.ReadAsync(cancellationToken))
        {
            parts.Add(new Model_SetupTech_SubordinatePart
            {
                WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                SequenceNo = Convert.ToInt32(reader["SequenceNo"], CultureInfo.InvariantCulture),
                SubPartId = reader["SubPartId"]?.ToString() ?? string.Empty,
                SubPartDesc = reader["SubPartDesc"] as string,
                RequiredQty = Convert.ToDecimal(reader["RequiredQty"], CultureInfo.InvariantCulture),
                QtyOnHand = Convert.ToDecimal(reader["QtyOnHand"], CultureInfo.InvariantCulture),
            });
        }

        return parts;
    }

    private static async Task ClearCachedSubordinatePartsAsync(MySqlConnection connection, string workOrderId, int sequenceNo, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM WorkOrderSubordinateParts WHERE WorkOrderId = @p_WorkOrderId AND SequenceNo = @p_SequenceNo";
        command.Parameters.AddWithValue("@p_WorkOrderId", workOrderId);
        command.Parameters.AddWithValue("@p_SequenceNo", sequenceNo);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertCachedSubordinatePartAsync(MySqlConnection connection, string workOrderId, int sequenceNo, Model_SetupTech_SubordinatePart subordinatePart, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_SetupTech_UpsertSubordinateParts(@p_WorkOrderId, @p_SequenceNo, @p_SubPartId, @p_SubPartDesc, @p_RequiredQty, @p_QtyOnHand, @p_CachedAt)";
        command.Parameters.AddWithValue("@p_WorkOrderId", workOrderId);
        command.Parameters.AddWithValue("@p_SequenceNo", sequenceNo);
        command.Parameters.AddWithValue("@p_SubPartId", subordinatePart.SubPartId);
        command.Parameters.AddWithValue("@p_SubPartDesc", subordinatePart.SubPartDesc ?? string.Empty);
        command.Parameters.AddWithValue("@p_RequiredQty", subordinatePart.RequiredQty);
        command.Parameters.AddWithValue("@p_QtyOnHand", subordinatePart.QtyOnHand);
        command.Parameters.AddWithValue("@p_CachedAt", DateTime.UtcNow);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
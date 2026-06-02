using System.Globalization;
using Microsoft.Data.SqlClient;
using MTM_Waitlist_Server.Api.Models;
using MTM_Waitlist_Server.Api.Models.InforVisual;
using MTM_Waitlist_Server.Core.Interfaces.Settings;

namespace MTM_Waitlist_Server.Api.Data;

/// <summary>
/// Provides read-only access to the Infor Visual SQL Server database for Feature 07 flows.
/// </summary>
public sealed class Dao_InforVisualWorkOrder
{
    private readonly IService_SettingsStore _settingsStore;

    /// <summary>
    /// Initialises a new instance with access to persisted Visual SQL connection settings.
    /// </summary>
    public Dao_InforVisualWorkOrder(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <summary>
    /// Returns all schedulable workcenters from Infor Visual.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_VisualWorkcenter>>> GetAllWorkcentersAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                sr.ID AS WorkcenterId,
                COALESCE(sr.DESCRIPTION, '') AS Description,
                COALESCE(sr.RESOURCE_TYPE, '') AS ResourceType,
                COALESCE(sr.DEPARTMENT_ID, '') AS DepartmentId,
                COALESCE(sr.SCHEDULE_GROUP_ID, '') AS ScheduleGroup
            FROM SHOP_RESOURCE sr
            WHERE sr.SCHEDULE_NORMALLY = 'Y'
            ORDER BY sr.ID;
            """;

        return await ExecuteReaderAsync(
            sql,
            static reader => new Model_VisualWorkcenter
            {
                WorkcenterId = reader["WorkcenterId"]?.ToString() ?? string.Empty,
                Description = reader["Description"]?.ToString() ?? string.Empty,
                ResourceType = reader["ResourceType"]?.ToString() ?? string.Empty,
                DepartmentId = reader["DepartmentId"]?.ToString() ?? string.Empty,
                ScheduleGroup = reader["ScheduleGroup"]?.ToString() ?? string.Empty,
            },
            cancellationToken);
    }

    /// <summary>
    /// Returns the header details for one work order identifier.
    /// </summary>
    public async Task<Model_Dao_Result<Model_VisualWorkOrderHeader>> GetWorkOrderHeaderAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
        {
            return Model_Dao_Result<Model_VisualWorkOrderHeader>.Failure("A work order ID is required.");
        }

        const string sql = """
            SELECT TOP (1)
                wo.BASE_ID AS WorkOrderId,
                COALESCE(wo.PART_ID, '') AS PartId,
                COALESCE(p.DESCRIPTION, '') AS PartDescription,
                COALESCE(p.TYPE, '') AS PartType,
                COALESCE(wo.STATUS, '') AS WorkOrderStatus,
                COALESCE(wo.DESIRED_QTY, 0) AS DesiredQty,
                wo.WANT_DATE AS WantDate,
                COALESCE(wo.SITE_ID, '') AS SiteId
            FROM WORK_ORDER wo
            LEFT JOIN PART p
                ON p.ID = wo.PART_ID
            WHERE wo.BASE_ID = @workOrderId
              AND wo.TYPE = 'W';
            """;

        var result = await ExecuteReaderAsync(
            sql,
            static reader => new Model_VisualWorkOrderHeader
            {
                WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                PartId = reader["PartId"]?.ToString() ?? string.Empty,
                PartDescription = reader["PartDescription"]?.ToString() ?? string.Empty,
                PartType = reader["PartType"]?.ToString() ?? string.Empty,
                WorkOrderStatus = reader["WorkOrderStatus"]?.ToString() ?? string.Empty,
                DesiredQty = Convert.ToDecimal(reader["DesiredQty"], CultureInfo.InvariantCulture),
                WantDate = ReadNullableDateTime(reader, "WantDate"),
                SiteId = reader["SiteId"]?.ToString() ?? string.Empty,
            },
            cancellationToken,
            CreateParameter("@workOrderId", workOrderId));

        if (!result.IsSuccess)
        {
            return Model_Dao_Result<Model_VisualWorkOrderHeader>.Failure(result.ErrorMessage);
        }

        var header = result.Data?.FirstOrDefault();
        return header is null
            ? Model_Dao_Result<Model_VisualWorkOrderHeader>.Failure($"Work order '{workOrderId}' was not found in Infor Visual.")
            : Model_Dao_Result<Model_VisualWorkOrderHeader>.Success(header);
    }

    /// <summary>
    /// Returns all sequences for one work order identifier.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_VisualWorkOrderSequence>>> GetWorkOrderSequencesAsync(string workOrderId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
        {
            return Model_Dao_Result<List<Model_VisualWorkOrderSequence>>.Failure("A work order ID is required.");
        }

        const string sql = """
            SELECT
                op.WORKORDER_BASE_ID AS WorkOrderId,
                op.SEQUENCE_NO AS SequenceNo,
                COALESCE(op.RESOURCE_ID, '') AS WorkcenterId,
                COALESCE(sr.DESCRIPTION, '') AS WorkcenterDescription,
                COALESCE(op.STATUS, '') AS SequenceStatus,
                CASE
                    WHEN COALESCE(CONVERT(varchar(10), op.SETUP_COMPLETED), 'N') IN ('1', 'Y', 'y', 'True', 'true') THEN CAST(1 AS bit)
                    ELSE CAST(0 AS bit)
                END AS SetupCompleted,
                COALESCE(op.COMPLETED_QTY, 0) AS CompletedQty,
                COALESCE(op.CALC_END_QTY, 0) AS TargetQty,
                op.SCHED_START_DATE AS SchedStart,
                op.SCHED_FINISH_DATE AS SchedFinish
            FROM OPERATION op
            LEFT JOIN SHOP_RESOURCE sr
                ON sr.ID = op.RESOURCE_ID
            WHERE op.WORKORDER_BASE_ID = @workOrderId
              AND op.WORKORDER_TYPE = 'W'
            ORDER BY op.SEQUENCE_NO;
            """;

        return await ExecuteReaderAsync(
            sql,
            static reader => new Model_VisualWorkOrderSequence
            {
                WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                SequenceNo = Convert.ToInt32(reader["SequenceNo"], CultureInfo.InvariantCulture),
                WorkcenterId = reader["WorkcenterId"]?.ToString() ?? string.Empty,
                WorkcenterDescription = reader["WorkcenterDescription"]?.ToString() ?? string.Empty,
                SequenceStatus = reader["SequenceStatus"]?.ToString() ?? string.Empty,
                SetupCompleted = Convert.ToBoolean(reader["SetupCompleted"], CultureInfo.InvariantCulture),
                CompletedQty = Convert.ToDecimal(reader["CompletedQty"], CultureInfo.InvariantCulture),
                TargetQty = Convert.ToDecimal(reader["TargetQty"], CultureInfo.InvariantCulture),
                SchedStart = ReadNullableDateTime(reader, "SchedStart"),
                SchedFinish = ReadNullableDateTime(reader, "SchedFinish"),
            },
            cancellationToken,
            CreateParameter("@workOrderId", workOrderId));
    }

    /// <summary>
    /// Returns all subordinate parts for one work order sequence.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_Visual_SubordinatePart>>> GetSubordinatePartsAsync(string workOrderId, int sequenceNo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workOrderId))
        {
            return Model_Dao_Result<List<Model_Visual_SubordinatePart>>.Failure("A work order ID is required.");
        }

        const string sql = """
            SELECT
                m.ORDER_ID AS WorkOrderId,
                m.OPERATION_SEQ_NO AS SequenceNo,
                COALESCE(m.PART_ID, '') AS PartId,
                COALESCE(p.DESCRIPTION, '') AS PartDescription,
                COALESCE(m.CALC_QTY, 0) AS RequiredQty,
                COALESCE(ps.QTY_ON_HAND, 0) AS QtyOnHand
            FROM WORK_ORDER_MATL m
            LEFT JOIN PART p
                ON p.ID = m.PART_ID
            LEFT JOIN PART_SITE ps
                ON ps.PART_ID = m.PART_ID
               AND ps.SITE_ID = '002'
            WHERE m.ORDER_ID = @workOrderId
              AND m.OPERATION_SEQ_NO = @sequenceNo
            ORDER BY m.PIECE_NO, m.PART_ID;
            """;

        return await ExecuteReaderAsync(
            sql,
            static reader => new Model_Visual_SubordinatePart
            {
                WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                SequenceNo = Convert.ToInt32(reader["SequenceNo"], CultureInfo.InvariantCulture),
                PartId = reader["PartId"]?.ToString() ?? string.Empty,
                PartDescription = reader["PartDescription"]?.ToString() ?? string.Empty,
                RequiredQty = Convert.ToDecimal(reader["RequiredQty"], CultureInfo.InvariantCulture),
                QtyOnHand = Convert.ToDecimal(reader["QtyOnHand"], CultureInfo.InvariantCulture),
            },
            cancellationToken,
            CreateParameter("@workOrderId", workOrderId),
            CreateParameter("@sequenceNo", sequenceNo));
    }

    /// <summary>
    /// Returns the currently active work orders for one workcenter resource.
    /// </summary>
    public async Task<Model_Dao_Result<List<Model_VisualWorkOrderHeader>>> GetActiveWorkOrdersForResourceAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceId))
        {
            return Model_Dao_Result<List<Model_VisualWorkOrderHeader>>.Failure("A workcenter ID is required.");
        }

        const string sql = """
            SELECT DISTINCT
                wo.BASE_ID AS WorkOrderId,
                COALESCE(wo.PART_ID, '') AS PartId,
                COALESCE(p.DESCRIPTION, '') AS PartDescription,
                COALESCE(p.TYPE, '') AS PartType,
                COALESCE(wo.STATUS, '') AS WorkOrderStatus,
                COALESCE(wo.DESIRED_QTY, 0) AS DesiredQty,
                wo.WANT_DATE AS WantDate,
                COALESCE(wo.SITE_ID, '') AS SiteId
            FROM OPERATION op
            INNER JOIN WORK_ORDER wo
                ON wo.BASE_ID = op.WORKORDER_BASE_ID
               AND wo.TYPE = op.WORKORDER_TYPE
            LEFT JOIN PART p
                ON p.ID = wo.PART_ID
            WHERE op.RESOURCE_ID = @resourceId
              AND op.STATUS IN ('O', 'R', 'S')
              AND wo.STATUS IN ('O', 'R')
            ORDER BY WantDate, WorkOrderId;
            """;

        return await ExecuteReaderAsync(
            sql,
            static reader => new Model_VisualWorkOrderHeader
            {
                WorkOrderId = reader["WorkOrderId"]?.ToString() ?? string.Empty,
                PartId = reader["PartId"]?.ToString() ?? string.Empty,
                PartDescription = reader["PartDescription"]?.ToString() ?? string.Empty,
                PartType = reader["PartType"]?.ToString() ?? string.Empty,
                WorkOrderStatus = reader["WorkOrderStatus"]?.ToString() ?? string.Empty,
                DesiredQty = Convert.ToDecimal(reader["DesiredQty"], CultureInfo.InvariantCulture),
                WantDate = ReadNullableDateTime(reader, "WantDate"),
                SiteId = reader["SiteId"]?.ToString() ?? string.Empty,
            },
            cancellationToken,
            CreateParameter("@resourceId", resourceId));
    }

    private async Task<Model_Dao_Result<List<T>>> ExecuteReaderAsync<T>(
        string sql,
        Func<SqlDataReader, T> map,
        CancellationToken cancellationToken,
        params SqlParameter[] parameters)
    {
        var validation = ValidateVisualSettings();
        if (!validation.IsSuccess)
        {
            return Model_Dao_Result<List<T>>.Failure(validation.ErrorMessage);
        }

        try
        {
            var settings = _settingsStore.Get().Visual;
            await using var connection = new SqlConnection(BuildConnectionString());
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = settings.CommandTimeout;
            if (parameters.Length > 0)
            {
                command.Parameters.AddRange(parameters);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(map(reader));
            }

            return Model_Dao_Result<List<T>>.Success(items);
        }
        catch (Exception ex)
        {
            return Model_Dao_Result<List<T>>.Failure($"Infor Visual is unavailable: {ex.Message}");
        }
    }

    private Model_Dao_Result ValidateVisualSettings()
    {
        var settings = _settingsStore.Get().Visual;
        if (!settings.Enabled)
        {
            return Model_Dao_Result.Failure("Infor Visual integration is disabled in server settings.");
        }

        if (string.IsNullOrWhiteSpace(settings.Host)
            || string.IsNullOrWhiteSpace(settings.DatabaseName)
            || string.IsNullOrWhiteSpace(settings.Username)
            || settings.Port <= 0)
        {
            return Model_Dao_Result.Failure("Infor Visual SQL connection settings are incomplete.");
        }

        return Model_Dao_Result.Success();
    }

    private string BuildConnectionString()
    {
        var settings = _settingsStore.Get().Visual;
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{settings.Host},{settings.Port}",
            InitialCatalog = settings.DatabaseName,
            UserID = settings.Username,
            Password = settings.Password,
            ConnectTimeout = settings.ConnectionTimeout,
            ApplicationIntent = ApplicationIntent.ReadOnly,
            Encrypt = false,
            TrustServerCertificate = true,
            IntegratedSecurity = false,
        };

        return builder.ConnectionString;
    }

    private static SqlParameter CreateParameter(string name, object value) => new(name, value);

    private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
    {
        return reader[columnName] is DBNull
            ? null
            : Convert.ToDateTime(reader[columnName], CultureInfo.InvariantCulture);
    }
}
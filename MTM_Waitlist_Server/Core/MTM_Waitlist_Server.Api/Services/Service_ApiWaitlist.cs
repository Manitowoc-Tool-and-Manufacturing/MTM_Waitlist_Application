using System.Globalization;
using MySqlConnector;
using MTM_Waitlist_Server.Core.Interfaces.Settings;

namespace MTM_Waitlist_Server.Api.Services;

/// <summary>
/// Implements the REST API waitlist CRUD flows used by the MAUI client.
/// Reads and writes waitlist entries exclusively through the MySQL stored procedure set.
/// </summary>
public sealed class Service_ApiWaitlist
{
    private readonly IService_SettingsStore _settingsStore;

    /// <summary>
    /// Initialises a new instance with access to persisted server database settings.
    /// </summary>
    public Service_ApiWaitlist(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <summary>
    /// Returns all waitlist entries ordered by the database procedure.
    /// </summary>
    public async Task<List<Model_ApiWaitlistEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Waitlist_GetAll(@p_Status, @p_RequestType, @p_Limit, @p_Offset)";
        command.Parameters.AddWithValue("@p_Status", DBNull.Value);
        command.Parameters.AddWithValue("@p_RequestType", DBNull.Value);
        command.Parameters.AddWithValue("@p_Limit", DBNull.Value);
        command.Parameters.AddWithValue("@p_Offset", DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var entries = new List<Model_ApiWaitlistEntry>();
        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(MapEntry(reader));
        }

        return entries;
    }

    /// <summary>
    /// Returns one waitlist entry by identifier, or <see langword="null"/> when missing.
    /// </summary>
    public async Task<Model_ApiWaitlistEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Waitlist_GetById(@p_Id)";
        command.Parameters.AddWithValue("@p_Id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapEntry(reader);
    }

    /// <summary>
    /// Creates a new waitlist entry and returns the database-populated row.
    /// </summary>
    public async Task<Model_ApiWaitlistEntry?> CreateAsync(Model_ApiWaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Waitlist_Insert(@p_WorkcenterName, @p_RequestType, @p_Status, @p_Priority, @p_Notes, @p_RequestedAt, @p_ScheduledAt, @p_AssignedToUserId, @p_CreatedByUserId, @p_NewId)";
        command.Parameters.AddWithValue("@p_WorkcenterName", entry.WorkcenterName);
        command.Parameters.AddWithValue("@p_RequestType", entry.RequestType.ToString());
        command.Parameters.AddWithValue("@p_Status", entry.Status.ToString());
        command.Parameters.AddWithValue("@p_Priority", entry.Priority);
        command.Parameters.AddWithValue("@p_Notes", entry.Notes);
        command.Parameters.AddWithValue("@p_RequestedAt", entry.RequestedAt == default ? DBNull.Value : entry.RequestedAt);
        command.Parameters.AddWithValue("@p_ScheduledAt", entry.ScheduledAt is null ? DBNull.Value : entry.ScheduledAt);
        command.Parameters.AddWithValue("@p_AssignedToUserId", entry.AssignedToUserId is null ? DBNull.Value : entry.AssignedToUserId);
        command.Parameters.AddWithValue("@p_CreatedByUserId", entry.CreatedByUserId is null ? DBNull.Value : entry.CreatedByUserId);

        var newIdParameter = new MySqlParameter("@p_NewId", MySqlDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output,
        };
        command.Parameters.Add(newIdParameter);

        await command.ExecuteNonQueryAsync(cancellationToken);

        if (newIdParameter.Value is null or DBNull)
        {
            return null;
        }

        var newId = Convert.ToInt32(newIdParameter.Value, CultureInfo.InvariantCulture);
        return await GetByIdAsync(newId, cancellationToken);
    }

    /// <summary>
    /// Updates an existing waitlist entry and returns the refreshed row.
    /// </summary>
    public async Task<Model_ApiWaitlistEntry?> UpdateAsync(Model_ApiWaitlistEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Waitlist_Update(@p_Id, @p_WorkcenterName, @p_RequestType, @p_Status, @p_Priority, @p_Notes, @p_RequestedAt, @p_ScheduledAt, @p_CompletedAt, @p_AssignedToUserId, @p_UpdatedByUserId)";
        command.Parameters.AddWithValue("@p_Id", entry.Id);
        command.Parameters.AddWithValue("@p_WorkcenterName", entry.WorkcenterName);
        command.Parameters.AddWithValue("@p_RequestType", entry.RequestType.ToString());
        command.Parameters.AddWithValue("@p_Status", entry.Status.ToString());
        command.Parameters.AddWithValue("@p_Priority", entry.Priority);
        command.Parameters.AddWithValue("@p_Notes", entry.Notes);
        command.Parameters.AddWithValue("@p_RequestedAt", entry.RequestedAt);
        command.Parameters.AddWithValue("@p_ScheduledAt", entry.ScheduledAt is null ? DBNull.Value : entry.ScheduledAt);
        command.Parameters.AddWithValue("@p_CompletedAt", entry.CompletedAt is null ? DBNull.Value : entry.CompletedAt);
        command.Parameters.AddWithValue("@p_AssignedToUserId", entry.AssignedToUserId is null ? DBNull.Value : entry.AssignedToUserId);
        command.Parameters.AddWithValue("@p_UpdatedByUserId", entry.UpdatedByUserId is null ? DBNull.Value : entry.UpdatedByUserId);

        await command.ExecuteNonQueryAsync(cancellationToken);
        return await GetByIdAsync(entry.Id, cancellationToken);
    }

    /// <summary>
    /// Deletes a waitlist entry by identifier.
    /// </summary>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Waitlist_Delete(@p_Id)";
        command.Parameters.AddWithValue("@p_Id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
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

    private static Model_ApiWaitlistEntry MapEntry(MySqlDataReader reader)
    {
        return new Model_ApiWaitlistEntry
        {
            Id = Convert.ToInt32(reader["Id"], CultureInfo.InvariantCulture),
            WorkcenterName = reader["WorkcenterName"]?.ToString() ?? string.Empty,
            RequestType = ParseRequestType(reader["RequestType"]?.ToString()),
            Status = ParseStatus(reader["Status"]?.ToString()),
            Priority = Convert.ToInt32(reader["Priority"], CultureInfo.InvariantCulture),
            Notes = reader["Notes"] as string,
            RequestedAt = Convert.ToDateTime(reader["RequestedAt"], CultureInfo.InvariantCulture),
            ScheduledAt = reader["ScheduledAt"] is DBNull ? null : Convert.ToDateTime(reader["ScheduledAt"], CultureInfo.InvariantCulture),
            CompletedAt = reader["CompletedAt"] is DBNull ? null : Convert.ToDateTime(reader["CompletedAt"], CultureInfo.InvariantCulture),
            AssignedToUserId = reader["AssignedToUserId"] is DBNull ? null : Convert.ToInt32(reader["AssignedToUserId"], CultureInfo.InvariantCulture),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"], CultureInfo.InvariantCulture),
            UpdatedAt = Convert.ToDateTime(reader["UpdatedAt"], CultureInfo.InvariantCulture),
            CreatedByUserId = reader["CreatedByUserId"] is DBNull ? null : Convert.ToInt32(reader["CreatedByUserId"], CultureInfo.InvariantCulture),
            UpdatedByUserId = reader["UpdatedByUserId"] is DBNull ? null : Convert.ToInt32(reader["UpdatedByUserId"], CultureInfo.InvariantCulture),
        };
    }

    private static Enum_ApiWaitlistRequestType ParseRequestType(string? value)
        => Enum.TryParse<Enum_ApiWaitlistRequestType>(value, ignoreCase: false, out var parsed)
            ? parsed
            : Enum_ApiWaitlistRequestType.Coil;

    private static Enum_ApiWaitlistStatus ParseStatus(string? value)
        => Enum.TryParse<Enum_ApiWaitlistStatus>(value, ignoreCase: false, out var parsed)
            ? parsed
            : Enum_ApiWaitlistStatus.Waiting;
}

/// <summary>
/// API DTO for a waitlist entry payload returned to or accepted from the MAUI client.
/// </summary>
public sealed class Model_ApiWaitlistEntry
{
    /// <summary>Unique identifier for the waitlist entry.</summary>
    public int Id { get; set; }

    /// <summary>Name of the workcenter that submitted the request.</summary>
    public string WorkcenterName { get; set; } = string.Empty;

    /// <summary>Category of logistics or material-handling request.</summary>
    public Enum_ApiWaitlistRequestType RequestType { get; set; }

    /// <summary>Current lifecycle state of the request.</summary>
    public Enum_ApiWaitlistStatus Status { get; set; } = Enum_ApiWaitlistStatus.Waiting;

    /// <summary>Sort priority. Lower numbers are more urgent.</summary>
    public int Priority { get; set; } = 5;

    /// <summary>Optional free-text notes.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC timestamp when the request was submitted.</summary>
    public DateTime RequestedAt { get; set; }

    /// <summary>Optional scheduled fulfillment timestamp.</summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>Optional completion timestamp.</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Optional assigned user identifier.</summary>
    public int? AssignedToUserId { get; set; }

    /// <summary>UTC timestamp when the record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp when the record was last updated.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Identifier of the user who created the record.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Identifier of the user who last updated the record.</summary>
    public int? UpdatedByUserId { get; set; }
}

/// <summary>
/// Supported waitlist request categories. Numeric order matches the MAUI client enum contract.
/// </summary>
public enum Enum_ApiWaitlistRequestType
{
    Coil,
    Dunnage,
    PickUpFinishedGoods,
    PickUpUnusedGoods,
    PickUpDunnage,
    BringPartsToPress,
    RemoveCoilFromPress,
    BringPickUpDie,
}

/// <summary>
/// Supported waitlist lifecycle states. Numeric order matches the MAUI client enum contract.
/// </summary>
public enum Enum_ApiWaitlistStatus
{
    Waiting,
    Active,
    Late,
    LowImportance,
    Project,
    Completed,
    Cancelled,
}
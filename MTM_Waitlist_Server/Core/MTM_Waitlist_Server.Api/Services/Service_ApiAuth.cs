using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using MTM_Waitlist_Server.Core.Interfaces.Settings;

namespace MTM_Waitlist_Server.Api.Services;

/// <summary>
/// Implements the REST API authentication flows used by the MAUI client.
/// Handles shared-workstation credential login, personal-workstation auto-login,
/// refresh-token rotation, and shared-workstation detection using MySQL stored procedures.
/// </summary>
public sealed class Service_ApiAuth
{
    private readonly IService_SettingsStore _settingsStore;

    /// <summary>
    /// Initialises a new instance with access to the persisted server settings.
    /// </summary>
    public Service_ApiAuth(IService_SettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
    }

    /// <summary>
    /// Attempts shared-workstation credential login using the application username and password.
    /// </summary>
    public async Task<Model_AuthResponse?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Auth_ValidateCredentials(@p_Username)";
        command.Parameters.AddWithValue("@p_Username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var passwordHash = reader["PasswordHash"]?.ToString() ?? string.Empty;
        if (!BCrypt.Net.BCrypt.Verify(password, passwordHash))
        {
            return null;
        }

        var userId = Convert.ToInt32(reader["Id"], CultureInfo.InvariantCulture);
        var displayName = reader["DisplayName"]?.ToString() ?? username;
        var role = reader["Role"]?.ToString() ?? string.Empty;
        await reader.DisposeAsync();

        return await CreateAuthResponseAsync(connection, userId, username, displayName, role, recordLogin: true, cancellationToken);
    }

    /// <summary>
    /// Attempts personal-workstation login using the Windows identity mapped in the Users table.
    /// </summary>
    public async Task<Model_AuthResponse?> AutoLoginAsync(string windowsUsername, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Auth_GetUserByWindowsUsername(@p_WindowsUsername)";
        command.Parameters.AddWithValue("@p_WindowsUsername", windowsUsername);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var userId = Convert.ToInt32(reader["Id"], CultureInfo.InvariantCulture);
        var username = reader["Username"]?.ToString() ?? windowsUsername;
        var displayName = reader["DisplayName"]?.ToString() ?? username;
        var role = reader["Role"]?.ToString() ?? string.Empty;
        await reader.DisposeAsync();

        return await CreateAuthResponseAsync(connection, userId, username, displayName, role, recordLogin: true, cancellationToken);
    }

    /// <summary>
    /// Determines whether the current workstation should force manual credential login.
    /// </summary>
    public async Task<Model_AuthLoginMode> CheckWorkstationAsync(string windowsUsername, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Auth_CheckSharedWorkstation(@p_WindowsUsername)";
        command.Parameters.AddWithValue("@p_WindowsUsername", windowsUsername);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new Model_AuthLoginMode(false, windowsUsername, null);
        }

        return new Model_AuthLoginMode(
            true,
            reader["WindowsUsername"]?.ToString() ?? windowsUsername,
            reader["MachineName"]?.ToString());
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access token and rotated refresh token.
    /// </summary>
    public async Task<Model_AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Auth_GetRefreshToken(@p_TokenHash)";
        command.Parameters.AddWithValue("@p_TokenHash", tokenHash);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        if (!(reader["IsActive"] is bool isActive) || !isActive)
        {
            return null;
        }

        var userId = Convert.ToInt32(reader["UserId"], CultureInfo.InvariantCulture);
        var username = reader["Username"]?.ToString() ?? string.Empty;
        var displayName = reader["DisplayName"]?.ToString() ?? username;
        var role = reader["Role"]?.ToString() ?? string.Empty;
        await reader.DisposeAsync();

        await RevokeRefreshTokenAsync(connection, tokenHash, cancellationToken);
        return await CreateAuthResponseAsync(connection, userId, username, displayName, role, recordLogin: false, cancellationToken);
    }

    /// <summary>
    /// Revokes the supplied refresh token hash if it exists.
    /// </summary>
    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashRefreshToken(refreshToken);

        await using var connection = OpenAppConnection();
        await connection.OpenAsync(cancellationToken);
        await RevokeRefreshTokenAsync(connection, tokenHash, cancellationToken);
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

    private async Task<Model_AuthResponse> CreateAuthResponseAsync(
        MySqlConnection connection,
        int userId,
        string username,
        string displayName,
        string role,
        bool recordLogin,
        CancellationToken cancellationToken)
    {
        if (recordLogin)
        {
            await using var recordLoginCommand = connection.CreateCommand();
            recordLoginCommand.CommandText = "CALL usp_Auth_RecordLogin(@p_UserId)";
            recordLoginCommand.Parameters.AddWithValue("@p_UserId", userId);
            await recordLoginCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var settings = _settingsStore.Get();
        var accessTokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(settings.Api.JwtExpiryMinutes);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(settings.Api.RefreshTokenExpiryDays);

        await using var saveRefreshCommand = connection.CreateCommand();
        saveRefreshCommand.CommandText = "CALL usp_Auth_SaveRefreshToken(@p_UserId, @p_TokenHash, @p_ExpiresAt)";
        saveRefreshCommand.Parameters.AddWithValue("@p_UserId", userId);
        saveRefreshCommand.Parameters.AddWithValue("@p_TokenHash", HashRefreshToken(refreshToken));
        saveRefreshCommand.Parameters.AddWithValue("@p_ExpiresAt", refreshTokenExpiresAt);
        await saveRefreshCommand.ExecuteNonQueryAsync(cancellationToken);

        return new Model_AuthResponse(
            CreateAccessToken(userId, username, displayName, role, settings.Api.JwtSecret, accessTokenExpiresAt),
            refreshToken,
            accessTokenExpiresAt,
            username,
            displayName,
            role);
    }

    private static async Task RevokeRefreshTokenAsync(MySqlConnection connection, string tokenHash, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "CALL usp_Auth_RevokeRefreshToken(@p_TokenHash)";
        command.Parameters.AddWithValue("@p_TokenHash", tokenHash);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }

    private static string CreateAccessToken(
        int userId,
        string username,
        string displayName,
        string role,
        string jwtSecret,
        DateTimeOffset expiresAt)
    {
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            throw new InvalidOperationException("The API JWT secret is not configured.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim(JwtRegisteredClaimNames.Name, displayName),
            new Claim(ClaimTypes.Role, role),
            new Claim("role", role),
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// API response returned after a successful authentication operation.
/// </summary>
public sealed record Model_AuthResponse(
    string Token,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string Username,
    string DisplayName,
    string Role);

/// <summary>
/// Indicates whether a workstation requires manual login or allows automatic Windows login.
/// </summary>
public sealed record Model_AuthLoginMode(
    bool IsSharedWorkstation,
    string WindowsUsername,
    string? MachineName);
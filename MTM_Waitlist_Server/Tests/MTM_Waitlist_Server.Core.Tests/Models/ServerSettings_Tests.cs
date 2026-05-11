using FluentAssertions;
using MTM_Waitlist_Server.Core.Models.Settings;
using System.Text.Json;

namespace MTM_Waitlist_Server.Core.Tests.Models;

/// <summary>
/// Verifies that all settings model defaults match the values specified in DATABASE-01
/// and that the models survive a JSON round-trip without data loss.
/// </summary>
public class ServerSettings_Tests
{
    // ── Defaults ───────────────────────────────────────────────────────────────

    [Fact]
    public void DatabaseSettings_Defaults_AreCorrect()
    {
        var s = new DatabaseSettings();
        s.Host.Should().Be("localhost");
        s.Port.Should().Be(3306);
        s.DatabaseName.Should().Be("mtm_waitlist");
        s.AppUsername.Should().Be("waitlist_admin_dbappuser");
        s.UpdaterUsername.Should().Be("waitlist_admin_dbupdater");
        s.ConnectionTimeout.Should().Be(10);
        s.CommandTimeout.Should().Be(30);
    }

    [Fact]
    public void ApiSettings_Defaults_AreCorrect()
    {
        var s = new ApiSettings();
        s.ListenAddress.Should().Be("http://0.0.0.0:5000");
        s.JwtExpiryMinutes.Should().Be(60);
        s.RefreshTokenExpiryDays.Should().Be(30);
    }

    [Fact]
    public void AdminSettings_DefaultGroup_IsBuiltinAdministrators()
    {
        var s = new AdminSettings();
        s.RequiredWindowsGroup.Should().Be(@"BUILTIN\Administrators");
    }

    [Fact]
    public void BackupSettings_Defaults_AreCorrect()
    {
        var s = new BackupSettings();
        s.BackupFolder.Should().Be(@"C:\MTM\WaitlistBackups\");
        s.MysqlDumpPath.Should().Be("mysqldump");
        s.RetentionDays.Should().Be(30);
        s.AutoBackupEnabled.Should().BeTrue();
        s.AutoBackupTime.Should().Be("02:00");
    }

    [Fact]
    public void MigrationsSettings_Defaults_AreCorrect()
    {
        var s = new MigrationsSettings();
        s.AutoApplyOnStartup.Should().BeFalse();
        s.MigrationFolder.Should().Be(@"database\migrations");
        s.ProceduresFolder.Should().Be(@"database\procedures");
    }

    [Fact]
    public void VisualSettings_Defaults_AreCorrect()
    {
        var s = new VisualSettings();
        s.Enabled.Should().BeFalse();
        s.Port.Should().Be(1433);
        s.Username.Should().Be("SHOP2");
        s.ConnectionTimeout.Should().Be(15);
        s.CommandTimeout.Should().Be(60);
    }

    [Fact]
    public void ServerSettings_ContainsAllSubsections()
    {
        var s = new ServerSettings();
        s.Database.Should().NotBeNull();
        s.Api.Should().NotBeNull();
        s.Admin.Should().NotBeNull();
        s.Backup.Should().NotBeNull();
        s.Migrations.Should().NotBeNull();
        s.Visual.Should().NotBeNull();
    }

    // ── JSON round-trip ────────────────────────────────────────────────────────

    [Fact]
    public void ServerSettings_JsonRoundTrip_PreservesValues()
    {
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNameCaseInsensitive = true };

        var original = new ServerSettings
        {
            Api = new ApiSettings { ListenAddress = "http://0.0.0.0:5000", JwtExpiryMinutes = 90 },
            Admin = new AdminSettings { RequiredWindowsGroup = @"BUILTIN\Users" },
            Database = new DatabaseSettings { Host = "192.168.1.10", Port = 3306 }
        };

        var json = JsonSerializer.Serialize(original, options);
        var restored = JsonSerializer.Deserialize<ServerSettings>(json, options)!;

        restored.Api.ListenAddress.Should().Be("http://0.0.0.0:5000");
        restored.Api.JwtExpiryMinutes.Should().Be(90);
        restored.Admin.RequiredWindowsGroup.Should().Be(@"BUILTIN\Users");
        restored.Database.Host.Should().Be("192.168.1.10");
    }

    [Fact]
    public void ServerSettings_JsonRoundTrip_MissingKeys_FallBackToDefaults()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        const string json = """{"Api":{"ListenAddress":"http://localhost:5000"}}""";

        var restored = JsonSerializer.Deserialize<ServerSettings>(json, options)!;

        // Keys not in the JSON should revert to class defaults.
        restored.Database.Port.Should().Be(3306);
        restored.Admin.RequiredWindowsGroup.Should().Be(@"BUILTIN\Administrators");
        restored.Backup.RetentionDays.Should().Be(30);
    }
}

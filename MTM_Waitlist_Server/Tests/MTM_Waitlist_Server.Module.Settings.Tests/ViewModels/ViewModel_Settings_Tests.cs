using FluentAssertions;
using Moq;
using MTM_Waitlist_Server.Core.Interfaces.Settings;
using MTM_Waitlist_Server.Core.Models.Settings;
using MTM_Waitlist_Server.Module.Settings.Services;
using MTM_Waitlist_Server.Module.Settings.ViewModels;

namespace MTM_Waitlist_Server.Module.Settings.Tests.ViewModels;

/// <summary>
/// Unit tests for <see cref="ViewModel_Settings"/>.
/// All disk I/O is replaced by a mocked <see cref="IService_SettingsStore"/>.
/// </summary>
public class ViewModel_Settings_Tests
{
    private static Mock<IService_SettingsStore> BuildStoreMock(ServerSettings? settings = null)
    {
        var s = settings ?? new ServerSettings();
        var mock = new Mock<IService_SettingsStore>();
        mock.Setup(x => x.Get()).Returns(s);
        mock.Setup(x => x.SaveAsync(It.IsAny<ServerSettings>())).Returns(Task.CompletedTask);
        mock.Setup(x => x.ReloadAsync()).Returns(Task.CompletedTask);
        return mock;
    }

    // ── Initial state ──────────────────────────────────────────────────────────

    [Fact]
    public void ViewModel_InitialState_IsNotSaving()
    {
        var vm = new ViewModel_Settings(BuildStoreMock().Object);
        vm.IsSaving.Should().BeFalse();
    }

    [Fact]
    public void ViewModel_InitialState_StatusMessage_IsEmpty()
    {
        var vm = new ViewModel_Settings(BuildStoreMock().Object);
        vm.StatusMessage.Should().BeEmpty();
    }

    // ── LoadAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_PopulatesDatabase_FromStore()
    {
        var stored = new ServerSettings
        {
            Database = new DatabaseSettings { Host = "10.0.0.1", Port = 3307 }
        };
        var vm = new ViewModel_Settings(BuildStoreMock(stored).Object);

        await vm.LoadCommand.ExecuteAsync(null);

        vm.Database.Host.Should().Be("10.0.0.1");
        vm.Database.Port.Should().Be(3307);
    }

    [Fact]
    public async Task LoadAsync_PopulatesAllCategories()
    {
        var stored = new ServerSettings
        {
            Api = new ApiSettings { ListenAddress = "http://0.0.0.0:6000" },
            Admin = new AdminSettings { RequiredWindowsGroup = @"DOMAIN\IT-Admins" },
            Notifications = new NotificationSettings { KillSwitchDefaultWarningSeconds = 60 }
        };
        var vm = new ViewModel_Settings(BuildStoreMock(stored).Object);

        await vm.LoadCommand.ExecuteAsync(null);

        vm.Api.ListenAddress.Should().Be("http://0.0.0.0:6000");
        vm.Admin.RequiredWindowsGroup.Should().Be(@"DOMAIN\IT-Admins");
        vm.Notifications.KillSwitchDefaultWarningSeconds.Should().Be(60);
    }

    [Fact]
    public async Task LoadAsync_SetsStatusMessage()
    {
        var vm = new ViewModel_Settings(BuildStoreMock().Object);

        await vm.LoadCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Be("Settings loaded.");
    }

    // ── SaveAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAsync_CallsStoreWithCurrentValues()
    {
        var mock = BuildStoreMock();
        var vm = new ViewModel_Settings(mock.Object);
        vm.Database.Host = "192.168.1.50";

        await vm.SaveCommand.ExecuteAsync(null);

        mock.Verify(x => x.SaveAsync(It.Is<ServerSettings>(s =>
            s.Database.Host == "192.168.1.50")), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_SetsStatusMessage_AfterSave()
    {
        var vm = new ViewModel_Settings(BuildStoreMock().Object);

        await vm.SaveCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Be("Settings saved.");
    }

    [Fact]
    public async Task SaveAsync_IsSaving_IsFalse_AfterCompletion()
    {
        var vm = new ViewModel_Settings(BuildStoreMock().Object);

        await vm.SaveCommand.ExecuteAsync(null);

        vm.IsSaving.Should().BeFalse();
    }

    [Fact]
    public async Task SaveAsync_IncludesNotifications_InSavedSettings()
    {
        var mock = BuildStoreMock();
        var vm = new ViewModel_Settings(mock.Object);
        vm.Notifications.KillSwitchDefaultWarningSeconds = 120;

        await vm.SaveCommand.ExecuteAsync(null);

        mock.Verify(x => x.SaveAsync(It.Is<ServerSettings>(s =>
            s.Notifications.KillSwitchDefaultWarningSeconds == 120)), Times.Once);
    }

    // ── ReloadAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReloadAsync_CallsReloadOnStore()
    {
        var mock = BuildStoreMock();
        var vm = new ViewModel_Settings(mock.Object);

        await vm.ReloadCommand.ExecuteAsync(null);

        mock.Verify(x => x.ReloadAsync(), Times.Once);
    }

    [Fact]
    public async Task ReloadAsync_SetsStatusMessage()
    {
        var vm = new ViewModel_Settings(BuildStoreMock().Object);

        await vm.ReloadCommand.ExecuteAsync(null);

        vm.StatusMessage.Should().Be("Settings reloaded from disk.");
    }
}

/// <summary>
/// Unit tests for <see cref="Service_SettingsValidator"/> covering all DATABASE-03 validation rules.
/// </summary>
public class Service_SettingsValidator_Tests
{
    private static Service_SettingsValidator BuildValidator() => new();

    // ── Valid settings ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_DefaultSettings_ReturnsNoErrors()
    {
        var errors = BuildValidator().Validate(new ServerSettings());
        errors.Should().BeEmpty();
    }

    // ── Database validation ────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyHost_ReturnsError()
    {
        var s = new ServerSettings { Database = new DatabaseSettings { Host = "" } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("Host is required"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    public void Validate_InvalidPort_ReturnsError(int port)
    {
        var s = new ServerSettings { Database = new DatabaseSettings { Port = port } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("Port") && e.Contains("out of range"));
    }

    [Theory]
    [InlineData("My Database")]
    [InlineData("MTM_Waitlist")]
    [InlineData("DB-01")]
    public void Validate_InvalidDatabaseName_ReturnsError(string name)
    {
        var s = new ServerSettings { Database = new DatabaseSettings { DatabaseName = name } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("DatabaseName"));
    }

    [Fact]
    public void Validate_EmptyAppUsername_ReturnsError()
    {
        var s = new ServerSettings { Database = new DatabaseSettings { AppUsername = "" } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("AppUsername is required"));
    }

    // ── API validation ─────────────────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyListenAddress_ReturnsError()
    {
        var s = new ServerSettings { Api = new ApiSettings { ListenAddress = "" } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("ListenAddress is required"));
    }

    [Fact]
    public void Validate_ZeroJwtExpiry_ReturnsError()
    {
        var s = new ServerSettings { Api = new ApiSettings { JwtExpiryMinutes = 0 } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("JwtExpiryMinutes"));
    }

    // ── Backup validation ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("2:00")]
    [InlineData("25:00")]
    [InlineData("2pm")]
    public void Validate_InvalidAutoBackupTime_ReturnsError(string time)
    {
        var s = new ServerSettings
        {
            Backup = new BackupSettings { AutoBackupEnabled = true, AutoBackupTime = time }
        };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("AutoBackupTime"));
    }

    [Fact]
    public void Validate_ValidAutoBackupTime_ReturnsNoBackupError()
    {
        var s = new ServerSettings
        {
            Backup = new BackupSettings { AutoBackupEnabled = true, AutoBackupTime = "02:00" }
        };
        BuildValidator().Validate(s).Should().NotContain(e => e.Contains("AutoBackupTime"));
    }

    // ── Visual validation ──────────────────────────────────────────────────────

    [Fact]
    public void Validate_VisualDisabled_NoHostError()
    {
        var s = new ServerSettings { Visual = new VisualSettings { Enabled = false, Host = "" } };
        BuildValidator().Validate(s).Should().NotContain(e => e.Contains("Visual: Host"));
    }

    [Fact]
    public void Validate_VisualEnabled_EmptyHost_ReturnsError()
    {
        var s = new ServerSettings { Visual = new VisualSettings { Enabled = true, Host = "" } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("Visual: Host is required"));
    }

    // ── Notifications validation ───────────────────────────────────────────────

    [Fact]
    public void Validate_ZeroKillSwitchWarning_ReturnsError()
    {
        var s = new ServerSettings { Notifications = new NotificationSettings { KillSwitchDefaultWarningSeconds = 0 } };
        BuildValidator().Validate(s).Should().ContainSingle(e => e.Contains("KillSwitchDefaultWarningSeconds"));
    }
}

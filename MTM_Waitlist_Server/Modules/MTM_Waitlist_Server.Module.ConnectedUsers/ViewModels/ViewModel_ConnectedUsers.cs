using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist_Server.Core.Interfaces.ConnectedUsers;
using MTM_Waitlist_Server.Core.Models.ConnectedUsers;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Module.ConnectedUsers.ViewModels;

/// <summary>
/// ViewModel for the Connected Users module page.
/// Displays all registered users with their workstation information and shows
/// a detail panel when a user is selected from the list.
/// </summary>
public partial class ViewModel_ConnectedUsers : ObservableObject
{
    private readonly IService_ConnectedUsers _connectedUsers;

    [ObservableProperty]
    private ObservableCollection<Model_ConnectedUser> _users = [];

    [ObservableProperty]
    private Model_ConnectedUser? _selectedUser;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>True when a user is selected and the detail panel should be visible.</summary>
    public bool HasSelectedUser => SelectedUser is not null;

    /// <summary>Human-friendly last-login text for the selected user's detail panel.</summary>
    public string SelectedUserLastLoginDisplay =>
        SelectedUser?.LastLoginAt?.ToLocalTime().ToString("dddd, MMMM d yyyy  h:mm tt")
        ?? "Never logged in";

    /// <summary>Human-friendly workstation text for the selected user's detail panel.</summary>
    public string SelectedUserWorkstationDisplay =>
        SelectedUser is null
            ? string.Empty
            : SelectedUser.IsSharedWorkstation
                ? $"{SelectedUser.WorkstationName ?? "Shared"} (Shared workstation)"
                : SelectedUser.WindowsUsername is not null
                    ? $"Personal workstation  ({SelectedUser.WindowsUsername})"
                    : "No workstation assigned";

    /// <summary>Human-friendly role label.</summary>
    public string SelectedUserRoleDisplay =>
        FormatRole(SelectedUser?.Role);

    public ViewModel_ConnectedUsers(IService_ConnectedUsers connectedUsers)
    {
        _connectedUsers = connectedUsers;
    }

    partial void OnSelectedUserChanged(Model_ConnectedUser? value)
    {
        // Raise change notifications for all computed detail properties.
        OnPropertyChanged(nameof(HasSelectedUser));
        OnPropertyChanged(nameof(SelectedUserLastLoginDisplay));
        OnPropertyChanged(nameof(SelectedUserWorkstationDisplay));
        OnPropertyChanged(nameof(SelectedUserRoleDisplay));
    }

    /// <summary>Loads (or refreshes) the user list from the database.</summary>
    [RelayCommand]
    private async Task LoadUsersAsync(CancellationToken ct = default)
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading users…";

        try
        {
            var all = await _connectedUsers.GetAllAsync(ct);
            Users = new ObservableCollection<Model_ConnectedUser>(all);
            StatusMessage = $"{Users.Count} user{(Users.Count == 1 ? "" : "s")} found";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Load cancelled.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load users: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string FormatRole(string? role) => role switch
    {
        "PressOperation"        => "Press Operation",
        "SetupTech"             => "Setup Technician",
        "ProductionSupervisor"  => "Production Supervisor",
        "ProductionManager"     => "Production Manager",
        "Quality"               => "Quality",
        "Receiving"             => "Receiving",
        "MaterialHandler"       => "Material Handler",
        "Admin"                 => "Administrator",
        "Developer"             => "Developer",
        null or ""              => "—",
        _                       => role,
    };
}

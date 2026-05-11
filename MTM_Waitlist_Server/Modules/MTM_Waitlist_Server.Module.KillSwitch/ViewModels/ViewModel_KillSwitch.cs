using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist_Server.Core.Interfaces.KillSwitch;
using MTM_Waitlist_Server.Core.Models.KillSwitch;
using System.Collections.ObjectModel;

namespace MTM_Waitlist_Server.Module.KillSwitch.ViewModels;

/// <summary>
/// ViewModel for the Kill Switch module page.
/// Manages graceful shutdown signals for connected MAUI clients.
/// </summary>
public partial class ViewModel_KillSwitch : ObservableObject
{
    private readonly IService_KillSwitch _killSwitch;

    [ObservableProperty] private ObservableCollection<ClientHeartbeat> _connectedClients = [];
    [ObservableProperty] private ObservableCollection<ActiveShutdownSignal> _activeSignals = [];
    [ObservableProperty] private int _selectedWarningSeconds = 60;
    [ObservableProperty] private string _shutdownMessage = "Server maintenance — please save your work.";
    [ObservableProperty] private bool _isShutdownActive;
    [ObservableProperty] private bool _isRestoreInProgress;

    public ViewModel_KillSwitch(IService_KillSwitch killSwitch)
    {
        _killSwitch = killSwitch;
    }

    private bool CanIssueShutdown() => !IsRestoreInProgress && !IsShutdownActive;

    /// <summary>Broadcasts a shutdown signal to all connected clients.</summary>
    [RelayCommand(CanExecute = nameof(CanIssueShutdown))]
    private async Task ShutDownAllAsync()
    {
        _killSwitch.SetShutdownSignal(ShutdownTarget.All, SelectedWarningSeconds, ShutdownMessage);
        await RefreshClientsAsync();
        IsShutdownActive = true;
        ShutDownAllCommand.NotifyCanExecuteChanged();
        ShutDownClientCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Broadcasts a shutdown signal to a single client by machine name.</summary>
    [RelayCommand(CanExecute = nameof(CanIssueShutdown))]
    private async Task ShutDownClientAsync(ClientHeartbeat client)
    {
        _killSwitch.SetShutdownSignal(ShutdownTarget.ByMachine, SelectedWarningSeconds,
            ShutdownMessage, machineName: client.MachineName);
        await RefreshClientsAsync();
    }

    /// <summary>Cancels all active shutdown signals.</summary>
    [RelayCommand]
    private async Task CancelShutdownAsync()
    {
        _killSwitch.CancelAllSignals();
        IsShutdownActive = false;
        ShutDownAllCommand.NotifyCanExecuteChanged();
        ShutDownClientCommand.NotifyCanExecuteChanged();
        await RefreshClientsAsync();
    }

    /// <summary>Refreshes the connected clients list from the kill-switch service.</summary>
    [RelayCommand]
    private Task RefreshClientsAsync()
    {
        ConnectedClients = new ObservableCollection<ClientHeartbeat>(_killSwitch.GetConnectedClients());
        ActiveSignals = new ObservableCollection<ActiveShutdownSignal>(_killSwitch.GetActiveSignals());
        IsRestoreInProgress = _killSwitch.IsRestoreInProgress;
        ShutDownAllCommand.NotifyCanExecuteChanged();
        ShutDownClientCommand.NotifyCanExecuteChanged();
        return Task.CompletedTask;
    }
}

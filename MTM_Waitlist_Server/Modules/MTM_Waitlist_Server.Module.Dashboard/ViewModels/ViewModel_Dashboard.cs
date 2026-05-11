using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTM_Waitlist_Server.Core.Interfaces.Dashboard;
using MTM_Waitlist_Server.Core.Models.Dashboard;
using System.Collections.ObjectModel;

namespace MTM_Waitlist_Server.Module.Dashboard.ViewModels;

/// <summary>
/// ViewModel for the MySQL status dashboard page.
/// Shows DB size, connection counts, table stats, active connections, and the activity log.
/// </summary>
public partial class ViewModel_Dashboard : ObservableObject
{
    private readonly IService_Dashboard _dashboard;
    private DateTime _startedAt = DateTime.UtcNow;

    [ObservableProperty] private TimeSpan _apiUptime;
    [ObservableProperty] private string _apiUptimeDisplay = "—";
    [ObservableProperty] private string _dbSizeMb = "—";
    [ObservableProperty] private int _activeConnectionCount;
    [ObservableProperty] private int _maxConnections;
    [ObservableProperty] private int _totalRequestsToday;
    [ObservableProperty] private int _openRequestCount;
    [ObservableProperty] private int _overdueRequestCount;
    [ObservableProperty] private ObservableCollection<Model_TableStat> _tableStats = [];
    [ObservableProperty] private ObservableCollection<Model_ActiveConnection> _activeConnections = [];
    [ObservableProperty] private ObservableCollection<LogEntry> _recentActivity = [];
    [ObservableProperty] private bool _isRefreshing;
    [ObservableProperty] private DateTime _lastRefreshedAt;

    public ViewModel_Dashboard(IService_Dashboard dashboard)
    {
        _dashboard = dashboard;
    }

    /// <summary>Refreshes all dashboard data from the database.</summary>
    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        if (IsRefreshing)
        {
            return;
        }

        IsRefreshing = true;

        try
        {
            ApiUptime = DateTime.UtcNow - _startedAt;
            ApiUptimeDisplay = $"{(int)ApiUptime.TotalHours}h {ApiUptime.Minutes:D2}m";

            var (active, max) = await _dashboard.GetConnectionCountsAsync(ct);
            ActiveConnectionCount = active;
            MaxConnections = max;

            var sizeBytes = await _dashboard.GetDatabaseSizeBytesAsync(ct);
            DbSizeMb = $"{sizeBytes / 1_048_576.0:F1} MB";

            var tableStats = await _dashboard.GetTableStatsAsync(ct);
            TableStats = new ObservableCollection<Model_TableStat>(tableStats);

            var connections = await _dashboard.GetActiveConnectionsAsync(ct);
            ActiveConnections = new ObservableCollection<Model_ActiveConnection>(connections);

            var (total, open, overdue) = await _dashboard.GetWaitlistCountsAsync(ct);
            TotalRequestsToday = total;
            OpenRequestCount = open;
            OverdueRequestCount = overdue;

            var activity = _dashboard.GetRecentActivity();
            RecentActivity = new ObservableCollection<LogEntry>(activity ?? []);

            LastRefreshedAt = DateTime.Now;
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>Refreshes table stats with exact COUNT(*) queries.</summary>
    [RelayCommand]
    private async Task RefreshExactAsync(CancellationToken ct)
    {
        var exact = await _dashboard.GetExactTableStatsAsync(ct);
        TableStats = new ObservableCollection<Model_TableStat>(exact);
    }

    /// <summary>Issues a MySQL KILL for the selected connection.</summary>
    [RelayCommand]
    private async Task KillConnectionAsync(Model_ActiveConnection connection)
    {
        await _dashboard.KillConnectionAsync(connection.ThreadId);
        await RefreshAsync(CancellationToken.None);
    }

    [RelayCommand]
    private Task StartAutoRefreshAsync(CancellationToken ct) =>
        // Refresh every 30 seconds until cancelled.
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                await RefreshAsync(ct);
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
            }
        }, ct);

    [RelayCommand]
    private Task OpenFullLogAsync() =>
        // TODO: open full log window.
        Task.CompletedTask;
}

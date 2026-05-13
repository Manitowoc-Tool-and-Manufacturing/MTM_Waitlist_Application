using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MTM_Waitlist_Server.Core.Models.KillSwitch;
using MTM_Waitlist_Server.Module.KillSwitch.ViewModels;

namespace MTM_Waitlist_Server.Module.KillSwitch.Views;

/// <summary>
/// Code-behind for the Kill Switch module page.
/// All logic is handled by <see cref="ViewModel_KillSwitch"/>.
/// </summary>
public sealed partial class View_KillSwitch : Page
{
    private readonly DispatcherTimer _refreshTimer = new()
    {
        Interval = TimeSpan.FromSeconds(5),
    };

    /// <summary>Typed accessor used by x:Bind expressions in the XAML.</summary>
    public ViewModel_KillSwitch ViewModel { get; }

    public View_KillSwitch(ViewModel_KillSwitch viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        _refreshTimer.Tick += OnRefreshTimerTick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshClients();
        _refreshTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _refreshTimer.Stop();
    }

    private void OnRefreshTimerTick(object? sender, object e)
    {
        RefreshClients();
    }

    private void RefreshClients()
    {
        if (ViewModel.RefreshClientsCommand.CanExecute(null))
        {
            ViewModel.RefreshClientsCommand.Execute(null);
        }
    }

    private void ShutDownClientButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ClientHeartbeat client })
        {
            ViewModel.ShutDownClientCommand.Execute(client);
        }
    }
}

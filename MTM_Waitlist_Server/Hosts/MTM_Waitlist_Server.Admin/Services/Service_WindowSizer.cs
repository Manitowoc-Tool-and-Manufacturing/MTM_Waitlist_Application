using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using MTM_Waitlist_Server.Core.Interfaces.Window;
using Windows.Graphics;

namespace MTM_Waitlist_Server.Admin.Services;

/// <summary>
/// Resizes the main application window to fit the active scenario.
/// Sizes were determined by measuring the rendered first-run wizard UI.
/// </summary>
internal sealed class Service_WindowSizer : IService_WindowSizer
{
    // Measured from the rendered Step 1 panel — tall enough to show all fields
    // plus the button row without requiring the user to scroll immediately.
    private const int FirstRunWidth  = 700;
    private const int FirstRunHeight = 900;

    // Comfortable default for the normal admin shell — wide enough to show
    // the dashboard tables without horizontal scrolling.
    private const int NormalWidth  = 1200;
    private const int NormalHeight = 750;

    private readonly AppWindow _appWindow;

    /// <summary>
    /// Initialises the sizer by obtaining the <see cref="AppWindow"/> for
    /// <paramref name="window"/>.
    /// </summary>
    public Service_WindowSizer(Window window)
    {
        var hWnd     = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow   = AppWindow.GetFromWindowId(windowId);
    }

    /// <inheritdoc/>
    public void ApplyFirstRunSize()
    {
        _appWindow.Resize(new SizeInt32(FirstRunWidth, FirstRunHeight));
        CenterOnMonitor();
    }

    /// <inheritdoc/>
    public void ApplyNormalSize()
    {
        _appWindow.Resize(new SizeInt32(NormalWidth, NormalHeight));
        CenterOnMonitor();
    }

    /// <inheritdoc/>
    public void CenterOnMonitor()
    {
        // Retrieve the work area (excludes taskbar) of the monitor the window is on.
        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Nearest);
        var workArea    = displayArea.WorkArea;
        var size        = _appWindow.Size;

        var x = workArea.X + (workArea.Width  - size.Width)  / 2;
        var y = workArea.Y + (workArea.Height - size.Height) / 2;

        _appWindow.Move(new PointInt32(x, y));
    }
}

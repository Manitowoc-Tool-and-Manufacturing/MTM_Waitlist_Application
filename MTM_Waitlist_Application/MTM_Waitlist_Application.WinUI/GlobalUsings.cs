// Global using aliases that resolve MAUI/WinUI namespace ambiguity.
// MAUI types bleed in transitively via Data and Services which use UseMaui=true.
// These aliases ensure every file in the WinUI host resolves to the correct WinUI type
// without requiring fully-qualified names throughout the codebase.

global using Application = Microsoft.UI.Xaml.Application;
global using Window = Microsoft.UI.Xaml.Window;
global using Visibility = Microsoft.UI.Xaml.Visibility;
global using Page = Microsoft.UI.Xaml.Controls.Page;
global using IValueConverter = Microsoft.UI.Xaml.Data.IValueConverter;
global using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

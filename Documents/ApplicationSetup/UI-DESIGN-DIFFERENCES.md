# UI Design Differences — Windows (WinUI 3) vs Android (.NET MAUI)

> **Solution:** MTM_Waitlist_Application  
> **Applies to:** `MTM_Waitlist_Application.WinUI` (WinUI 3) vs `MTM_Waitlist_Application.Droid` (.NET MAUI)  
> **Shared UI lives in:** `MTM_Waitlist_Application` (shared project)

> ⚠️ **Platform distinction:** The **Windows host** is a standalone **WinUI 3** application (`Microsoft.UI.Xaml`). It does **not** use MAUI `ContentPage`, `Shell`, `FlyoutItem`, `TabBar`, `CollectionView`, or `AppThemeBinding`. The **Android host** is a full **.NET MAUI** application and retains all MAUI patterns. ViewModels (CommunityToolkit.Mvvm) are shared across both platforms.

---

## 🧠 Core Principle

> The **ViewModel is always shared**. Only the **View (XAML)** changes per platform.

```
MTM_Waitlist_Application          ← Shared ViewModel (same for both)
       │
       ├── WaitlistPage.Windows.xaml    ← Rich desktop layout
       └── WaitlistPage.Mobile.xaml     ← Simplified mobile layout
```

Both XAML files bind to the **exact same ViewModel** — zero logic duplication.

---

## 1. 📐 Layout Philosophy

### Windows — Data Dense, Space Rich (WinUI 3)

Windows Views are WinUI 3 **Pages** (`Microsoft.UI.Xaml.Controls.Page`). Use WinUI 3 controls: `StackPanel`, `ListView` / `ItemsRepeater`, `TextBlock`, `TextBox`, `Button` (with `Content=` not `Text=`). Sidebar navigation lives in `MainWindow.xaml` via `NavigationView` — individual pages do not need to recreate it.

```xml
<!-- Feature.Waitlist / Views / WaitlistEntry / View_Waitlist_Entry.Windows.xaml -->
<!-- WinUI 3 Page — Microsoft.UI.Xaml namespace -->
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      xmlns:models="using:Core.Models.Waitlist"
      x:Class="Feature.Waitlist.Views.WaitlistEntry.View_Waitlist_Entry">

    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="300" />
        </Grid.ColumnDefinitions>

        <!-- Main data list -->
        <ListView Grid.Column="0"
                  ItemsSource="{x:Bind ViewModel.WaitlistEntries, Mode=OneWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="models:Model_WaitlistEntry">
                    <Grid Padding="8">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="120" />
                        </Grid.ColumnDefinitions>
                        <TextBlock Grid.Column="0" Text="{x:Bind Name}" />
                        <TextBlock Grid.Column="1" Text="{x:Bind Date}" />
                        <TextBlock Grid.Column="2" Text="{x:Bind Status}" />
                        <Button Grid.Column="3" Content="Approve"
                                Command="{x:Bind ViewModel.ApproveCommand, Mode=OneWay}"
                                CommandParameter="{x:Bind}" />
                    </Grid>
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>

        <!-- Detail panel -->
        <StackPanel Grid.Column="1" Padding="16" Spacing="8">
            <TextBlock Text="Selected Entry" FontSize="18" FontWeight="Bold" />
            <TextBlock Text="{x:Bind ViewModel.SelectedEntry.Name, Mode=OneWay}" />
            <TextBlock Text="{x:Bind ViewModel.SelectedEntry.Notes, Mode=OneWay}" />
        </StackPanel>
    </Grid>

</Page>
```

> Note: The typed `ViewModel` property is exposed by the code-behind — see Section 5.

### Android — Single Column, Thumb Friendly (.NET MAUI)

```xml
<!-- Feature.Waitlist / Views / WaitlistEntry / View_Waitlist_Entry.Android.xaml -->
<!-- .NET MAUI ContentPage — Android host only -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Feature.Waitlist.ViewModels"
             x:DataType="vm:ViewModel_Waitlist_Entry">

<Shell>
    <TabBar>
        <Tab Title="Waitlist" Icon="list.png">
            <ShellContent>
                <StackLayout Padding="16" Spacing="12">

                    <Label Text="Waitlist" FontSize="22" FontAttributes="Bold"/>

                    <SearchBar Placeholder="Search entries..."
                               Text="{Binding SearchQuery}"/>

                    <CollectionView ItemsSource="{Binding WaitlistEntries}">
                        <CollectionView.ItemTemplate>
                            <DataTemplate>
                                <!-- One card per entry, full width, large tap target -->
                                <Frame Margin="0,4" Padding="16" CornerRadius="8">
                                    <Grid RowDefinitions="Auto,Auto">
                                        <Label Grid.Row="0"
                                               Text="{Binding Name}"
                                               FontSize="16"
                                               FontAttributes="Bold"/>
                                        <Label Grid.Row="1"
                                               Text="{Binding Status}"
                                               TextColor="Gray"/>
                                    </Grid>
                                </Frame>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>

                </StackLayout>
            </ShellContent>
        </Tab>
    </TabBar>
</Shell>

</ContentPage>
```

---

## 2. 🧭 Navigation Patterns

| | Windows — WinUI 3 | Android — .NET MAUI |
|---|---|---|
| **Pattern** | `NavigationView` sidebar in `MainWindow.xaml` | Bottom `TabBar` in `AppShell.xaml` |
| **Framework tag** | `<NavigationViewItem>` | `<Tab>` inside `<TabBar>` |
| **Navigate to page** | `Frame.Navigate(typeof(View_X))` | `Shell.Current.GoToAsync("route")` |
| **Go back** | `Frame.GoBack()` | `Shell.Current.GoToAsync("..")` |
| **Depth** | Multi-level `NavigationViewItem` OK | Max 2–3 levels |
| **Back button** | `NavigationView` back button (`IsBackEnabled`) | Always present (hardware + soft) |
| **Context menus** | `MenuFlyout` on right-click | `SwipeView` for swipe actions |

### Windows Navigation (MainWindow.xaml — WinUI 3)

The Windows host does **not** use `AppShell.xaml` or MAUI `Shell`. Navigation is handled by a `NavigationView` in `MainWindow.xaml` with a `Frame` that hosts WinUI 3 `Page` instances.

```xml
<!-- MTM_Waitlist_Application.WinUI / MainWindow.xaml -->
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="MTM_Waitlist_Application.WinUI.MainWindow">

    <NavigationView x:Name="NavView"
                    PaneDisplayMode="Left"
                    IsBackButtonVisible="Collapsed"
                    SelectionChanged="NavView_SelectionChanged">

        <NavigationView.MenuItems>
            <NavigationViewItem Content="Dashboard" Icon="&#xE80F;" Tag="dashboard"/>
            <NavigationViewItem Content="Waitlist"  Icon="&#xE8FD;" Tag="waitlist"/>
            <NavigationViewItem Content="Reports"   Icon="&#xE9D2;" Tag="reports"/>
        </NavigationView.MenuItems>

        <!-- Host frame — WinUI 3 pages are loaded here via Frame.Navigate() -->
        <Frame x:Name="ShellFrame"/>

    </NavigationView>
</Window>
```

```csharp
// MainWindow.xaml.cs — navigate on item selection
private void NavView_SelectionChanged(NavigationView sender,
    NavigationViewSelectionChangedEventArgs args)
{
    if (args.SelectedItem is NavigationViewItem item)
    {
        Type? page = item.Tag?.ToString() switch
        {
            "dashboard" => typeof(View_Dashboard_Main),
            "waitlist"  => typeof(View_Waitlist_Entry),
            "reports"   => typeof(View_Reports_Main),
            _           => null
        };
        if (page is not null)
            ShellFrame.Navigate(page);
    }
}
```

### Android Shell (AppShell.xaml — .NET MAUI)

Android uses the standard MAUI `AppShell.xaml` with `TabBar` bottom navigation. `AppShell.xaml` is **Android-only** — the Windows host uses `NavigationView` in `MainWindow.xaml` instead.

```xml
<!-- MTM_Waitlist_Application / AppShell.xaml — Android MAUI only -->
<Shell xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
       xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">
    <TabBar>

        <Tab Title="Home" Icon="home.png">
            <ShellContent Route="home"
                          ContentTemplate="{DataTemplate views:HomePage}"/>
        </Tab>

        <Tab Title="Waitlist" Icon="list.png">
            <ShellContent Route="waitlist"
                          ContentTemplate="{DataTemplate views:WaitlistPage}"/>
        </Tab>

        <Tab Title="Profile" Icon="profile.png">
            <ShellContent Route="profile"
                          ContentTemplate="{DataTemplate views:ProfilePage}"/>
        </Tab>

    </TabBar>
</Shell>
```

> ⚠️ `Shell.Current.GoToAsync()` and `Shell.Current.GoToAsync("..")` are **Android (MAUI) only**.
> On Windows, navigate via `Frame.Navigate(typeof(Page))` and `Frame.GoBack()`.

---

## 3. 🎛️ Control Comparison

| Task | Windows Control (WinUI 3) | Android Control (.NET MAUI) |
|------|----------------|----------------|
| Show list of records | `ListView` or `ItemsRepeater` multi-column | `CollectionView` single-column cards |
| Navigation | `NavigationView` + `Frame.Navigate()` in `MainWindow` | `TabBar` bottom tabs in `AppShell` |
| Actions on items | Toolbar `CommandBar` buttons + right-click `MenuFlyout` | `SwipeView` left/right swipe |
| Form input | Side-by-side `TextBlock` + `TextBox` | Stacked `Label` above `Entry` |
| Date selection | `CalendarDatePicker` or WinUI `DatePicker` | `DatePicker` (triggers native bottom sheet) |
| Confirmation dialog | `ContentDialog` | `DisplayAlert()` (renders natively) |
| Loading indicator | `ProgressRing` in corner | `ActivityIndicator` full-screen overlay |
| Search | `AutoSuggestBox` in toolbar | `SearchBar` pinned top of scroll view |

---

## 4. 📂 How to Physically Separate UI Files

### Recommended: Separate XAML Files Per Platform

```
MTM_Waitlist_Application/
└── Views/
    ├── WaitlistPage.xaml            ← Default (used if no platform match)
    ├── WaitlistPage.Windows.xaml    ← Windows-specific layout
    ├── WaitlistPage.Android.xaml    ← Android-specific layout
    └── WaitlistPage.xaml.cs         ← Shared code-behind (same ViewModel binding)
```

> The shared project automatically picks up the right XAML file at compile time
> based on the target platform.

### Alternative: OnIdiom for Minor Tweaks Only

```xml
<!-- ✅ OK for small differences like font size, padding, margin -->
<Label FontSize="{OnIdiom Desktop=18, Phone=14}"
       Margin="{OnIdiom Desktop='24,8', Phone='12,4'}"
       Text="{Binding Title}"/>

<!-- ✅ OK for showing/hiding an element -->
<Button Text="Advanced Options"
        IsVisible="{OnIdiom Desktop=True, Phone=False}"/>

<!-- ❌ AVOID using OnIdiom for entire layout sections — gets messy fast -->
```

---

## 5. 🧠 The Shared ViewModel Pattern

The ViewModel lives in the **shared project** and is used by both platforms:

```csharp
// MTM_Waitlist_Application / ViewModels / WaitlistViewModel.cs
// ✅ Shared — referenced by BOTH Windows and Android views

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MTM_Waitlist_Application.ViewModels;

public partial class WaitlistViewModel : ObservableObject
{
    // Properties — auto-generate INotifyPropertyChanged via source generator
    [ObservableProperty] private ObservableCollection<WaitlistEntry> waitlistEntries = [];
    [ObservableProperty] private WaitlistEntry? selectedEntry;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string searchQuery = string.Empty;

    // Commands — auto-generate ICommand via source generator
    [RelayCommand]
    async Task LoadEntriesAsync()
    {
        IsLoading = true;
        // Same data loading logic for Windows AND Android
        WaitlistEntries = await _service.GetWaitlistAsync();
        IsLoading = false;
    }

    [RelayCommand]
    async Task ApproveEntryAsync(WaitlistEntry entry)
    {
        await _service.ApproveAsync(entry.Id);
        await LoadEntriesAsync();
    }
}
```

```xml
<!-- Windows XAML — WinUI 3 Page, uses x:Bind against typed ViewModel property -->
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      x:Class="Feature.Waitlist.Views.WaitlistEntry.View_Waitlist_Entry">
    <!-- Rich desktop layout using {x:Bind ViewModel.Property, Mode=OneWay} -->
    <ListView ItemsSource="{x:Bind ViewModel.WaitlistEntries, Mode=OneWay}" />
</Page>
```

```csharp
// Windows code-behind — expose typed ViewModel for x:Bind
public sealed partial class View_Waitlist_Entry : Page
{
    public ViewModel_Waitlist_Entry ViewModel { get; }

    public View_Waitlist_Entry(ViewModel_Waitlist_Entry viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
```

```xml
<!-- Android XAML — MAUI ContentPage with compiled x:DataType bindings -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:vm="clr-namespace:Feature.Waitlist.ViewModels"
             x:DataType="vm:ViewModel_Waitlist_Entry">
    <!-- Simplified mobile layout using {Binding Property, Mode=OneWay} -->
</ContentPage>
```

---

## 6. 📏 Sizing & Spacing Guidelines

| Property | Windows | Android |
|----------|---------|---------|
| **Font size (body)** | 14–16 | 14–16 |
| **Font size (header)** | 22–28 | 20–24 |
| **Button height** | 36–44 | **48 minimum** (thumb target) |
| **Page padding** | 24–32 | 12–16 |
| **List item height** | 40–52 | **56 minimum** |
| **Icon size** | 20–24 | 24–28 |
| **Column count in lists** | 3–6 | 1 (always) |

---

## 7. 🎨 Theming — Platform-Specific Approach

Theming is **not shared** between the two hosts. WinUI 3 and MAUI have different resource systems.

### Windows (WinUI 3) — ThemeDictionaries

```xml
<!-- MTM_Waitlist_Application.WinUI / App.xaml -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
                <SolidColorBrush x:Key="BackgroundBrush" Color="White"/>
                <SolidColorBrush x:Key="SurfaceBrush"   Color="#F3F3F3"/>
            </ResourceDictionary>
            <ResourceDictionary x:Key="Dark">
                <SolidColorBrush x:Key="BackgroundBrush" Color="#1E1E1E"/>
                <SolidColorBrush x:Key="SurfaceBrush"   Color="#2D2D2D"/>
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>

        <!-- Brand colours — shared across themes -->
        <SolidColorBrush x:Key="PrimaryBrush" Color="#0078D4"/>

        <!-- Shared button style -->
        <Style x:Key="PrimaryButton" TargetType="Button">
            <Setter Property="Background"    Value="{StaticResource PrimaryBrush}"/>
            <Setter Property="Foreground"    Value="White"/>
            <Setter Property="CornerRadius"  Value="6"/>
            <Setter Property="Height"        Value="44"/>
        </Style>
    </ResourceDictionary>
</Application.Resources>
```

> Use `{ThemeResource BackgroundBrush}` in WinUI 3 XAML to automatically switch between Light/Dark.

### Android (.NET MAUI) — AppThemeBinding

```xml
<!-- MTM_Waitlist_Application / App.xaml (Shared or Droid) — Android MAUI only -->
<Application.Resources>
    <ResourceDictionary>

        <!-- Platform-aware colours via AppThemeBinding -->
        <Color x:Key="BackgroundColor">
            <AppThemeBinding Light="White" Dark="#1E1E1E"/>
        </Color>
        <Color x:Key="PrimaryColor">#0078D4</Color>

        <Style x:Key="PrimaryButton" TargetType="Button">
            <Setter Property="BackgroundColor" Value="{StaticResource PrimaryColor}"/>
            <Setter Property="TextColor"       Value="White"/>
            <Setter Property="CornerRadius"    Value="6"/>
            <Setter Property="HeightRequest"   Value="44"/>
        </Style>

    </ResourceDictionary>
</Application.Resources>
```

---

## 8. 📋 Quick Reference Cheat Sheet

```
WINDOWS UI RULES (WinUI 3)          ANDROID UI RULES (.NET MAUI)
──────────────────────────          ─────────────────────────────
✅ Multi-column Grid layouts        ✅ Single-column StackLayout
✅ NavigationView sidebar           ✅ Bottom TabBar navigation
✅ ListView / ItemsRepeater tables  ✅ Card-based CollectionView
✅ Right-click MenuFlyout           ✅ SwipeView for actions
✅ CommandBar with icons            ✅ Floating Action Button (FAB)
✅ Side-by-side TextBlock+TextBox   ✅ Stacked label-above-input forms
✅ Smaller tap targets OK           ✅ Min 48px tap targets ALWAYS
✅ Hover states (VisualState)       ✅ No hover — touch only
✅ Keyboard shortcuts (Ctrl+S etc.) ✅ Hardware back button support
✅ Resizable window layouts         ✅ Fixed portrait-first layouts
```

---

## 📚 Further Reading

### WinUI 3 (Windows Host)
- [WinUI 3 Overview](https://learn.microsoft.com/en-us/windows/apps/winui/winui3/)
- [NavigationView control](https://learn.microsoft.com/en-us/windows/apps/design/controls/navigationview)
- [WinUI 3 Controls Gallery](https://learn.microsoft.com/en-us/windows/apps/design/controls/)
- [x:Bind — Compiled Bindings (WinUI 3)](https://learn.microsoft.com/en-us/windows/uwp/data-binding/data-binding-in-depth)
- [ThemeResource and ResourceDictionary](https://learn.microsoft.com/en-us/windows/apps/design/style/xaml-resource-dictionary)
- [Frame.Navigate — WinUI 3 page navigation](https://learn.microsoft.com/en-us/windows/apps/design/basics/navigate-between-two-pages)

### .NET MAUI (Android Host)
- [MAUI Shell Navigation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/)
- [OnPlatform / OnIdiom](https://learn.microsoft.com/en-us/dotnet/maui/xaml/markup-extensions/consume#onidiom-markup-extension)
- [MAUI Layouts Guide](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/layouts/)
- [CollectionView](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/collectionview/)
- [SwipeView](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/swipeview)

### Shared (Both Platforms)
- [MVVM Community Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
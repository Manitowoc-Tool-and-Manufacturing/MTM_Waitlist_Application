# UI Design Differences — Windows vs Android in .NET MAUI

> **Solution:** MTM_Waitlist_Application  
> **Applies to:** `MTM_Waitlist_Application.WinUI` vs `MTM_Waitlist_Application.Droid`  
> **Shared UI lives in:** `MTM_Waitlist_Application` (shared project)

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

### Windows — Data Dense, Space Rich

```xml
<!-- MTM_Waitlist_Application / Views / WaitlistPage.Windows.xaml -->
<Grid ColumnDefinitions="260,*,300" RowDefinitions="Auto,*">

    <!-- Header bar -->
    <Label Grid.ColumnSpan="3" Text="MTM Waitlist Manager"
           FontSize="24" Margin="16,12"/>

    <!-- Sidebar navigation -->
    <StackLayout Grid.Row="1" Grid.Column="0"
                 BackgroundColor="{AppThemeBinding Light=#F3F3F3, Dark=#1E1E1E}"
                 Padding="12">
        <Button Text="Dashboard" StyleClass="NavButton"/>
        <Button Text="Waitlist"  StyleClass="NavButton"/>
        <Button Text="Reports"   StyleClass="NavButton"/>
    </StackLayout>

    <!-- Main data table -->
    <CollectionView Grid.Row="1" Grid.Column="1"
                    ItemsSource="{Binding WaitlistEntries}">
        <CollectionView.ItemTemplate>
            <DataTemplate>
                <Grid ColumnDefinitions="*,*,*,120" Padding="8">
                    <Label Grid.Column="0" Text="{Binding Name}"/>
                    <Label Grid.Column="1" Text="{Binding Date}"/>
                    <Label Grid.Column="2" Text="{Binding Status}"/>
                    <Button Grid.Column="3" Text="Approve"
                            Command="{Binding Source={RelativeSource AncestorType={x:Type vm:WaitlistViewModel}},
                                              Path=ApproveCommand}"
                            CommandParameter="{Binding .}"/>
                </Grid>
            </DataTemplate>
        </CollectionView.ItemTemplate>
    </CollectionView>

    <!-- Detail panel -->
    <StackLayout Grid.Row="1" Grid.Column="2" Padding="16">
        <Label Text="Selected Entry" FontSize="18" FontAttributes="Bold"/>
        <Label Text="{Binding SelectedEntry.Name}"/>
        <Label Text="{Binding SelectedEntry.Notes}"/>
    </StackLayout>

</Grid>
```

### Android — Single Column, Thumb Friendly

```xml
<!-- MTM_Waitlist_Application / Views / WaitlistPage.Mobile.xaml -->
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
```

---

## 2. 🧭 Navigation Patterns

| | Windows (WinUI) | Android (Droid) |
|---|---|---|
| **Pattern** | Flyout sidebar | Bottom `TabBar` |
| **Shell tag** | `<FlyoutItem>` | `<Tab>` inside `<TabBar>` |
| **Depth** | Multi-level menus OK | Max 2–3 levels |
| **Back button** | Rarely needed | Always present (hardware + soft) |
| **Context menus** | `MenuFlyout` on right-click | `SwipeView` for swipe actions |

### Windows Shell (AppShell.xaml — WinUI)
```xml
<Shell FlyoutDisplayOptions="AsMultipleItems"
       FlyoutWidth="260">

    <FlyoutItem Title="Dashboard" Icon="dashboard.png">
        <ShellContent Route="dashboard"
                      ContentTemplate="{DataTemplate views:DashboardPage}"/>
    </FlyoutItem>

    <FlyoutItem Title="Waitlist" Icon="list.png">
        <ShellContent Route="waitlist"
                      ContentTemplate="{DataTemplate views:WaitlistPage}"/>
    </FlyoutItem>

    <FlyoutItem Title="Reports" Icon="reports.png">
        <ShellContent Route="reports"
                      ContentTemplate="{DataTemplate views:ReportsPage}"/>
    </FlyoutItem>

</Shell>
```

### Android Shell (AppShell.xaml — Droid)
```xml
<Shell>
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

> ⚠️ **The route names (`waitlist`, `dashboard`) must match** between both Shell
> files so shared navigation commands work identically.

---

## 3. 🎛️ Control Comparison

| Task | Windows Control | Android Control |
|------|----------------|----------------|
| Show list of records | `CollectionView` multi-column grid | `CollectionView` single-column cards |
| Navigation | `FlyoutItem` sidebar | `TabBar` bottom tabs |
| Actions on items | Toolbar buttons + right-click `MenuFlyout` | `SwipeView` left/right swipe |
| Form input | Side-by-side `Label` + `Entry` | Stacked `Label` above `Entry` |
| Date selection | `DatePicker` inline in form | `DatePicker` (triggers native bottom sheet) |
| Confirmation dialog | `DisplayAlert()` | `DisplayAlert()` (renders natively per platform) |
| Loading indicator | `ActivityIndicator` in corner | `ActivityIndicator` full-screen overlay |
| Search | `SearchBar` in toolbar | `SearchBar` pinned top of scroll view |

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
<!-- Windows XAML — binds to same ViewModel -->
<ContentPage xmlns:vm="clr-namespace:MTM_Waitlist_Application.ViewModels"
             x:DataType="vm:WaitlistViewModel">
    <!-- Rich desktop layout -->
</ContentPage>

<!-- Android XAML — binds to same ViewModel -->
<ContentPage xmlns:vm="clr-namespace:MTM_Waitlist_Application.ViewModels"
             x:DataType="vm:WaitlistViewModel">
    <!-- Simplified mobile layout -->
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

## 7. 🎨 Theming — Works the Same Way on Both

```xml
<!-- App.xaml in the shared project — applies to both platforms -->
<Application.Resources>
    <ResourceDictionary>

        <!-- Colours -->
        <Color x:Key="PrimaryColor">#0078D4</Color>
        <Color x:Key="SecondaryColor">#F3F3F3</Color>

        <!-- Platform-aware colours -->
        <Color x:Key="BackgroundColor">
            <AppThemeBinding Light="White" Dark="#1E1E1E"/>
        </Color>

        <!-- Shared button style — renders natively on each platform -->
        <Style x:Key="PrimaryButton" TargetType="Button">
            <Setter Property="BackgroundColor" Value="{StaticResource PrimaryColor}"/>
            <Setter Property="TextColor" Value="White"/>
            <Setter Property="CornerRadius" Value="6"/>
            <Setter Property="HeightRequest" Value="44"/>
        </Style>

    </ResourceDictionary>
</Application.Resources>
```

> Styles defined in the shared `App.xaml` apply automatically to both
> Windows and Android — no duplication needed.

---

## 8. 📋 Quick Reference Cheat Sheet

```
WINDOWS UI RULES                    ANDROID UI RULES
─────────────────────               ─────────────────────
✅ Multi-column Grid layouts        ✅ Single-column StackLayout
✅ Sidebar flyout navigation        ✅ Bottom TabBar navigation
✅ Dense data tables                ✅ Card-based CollectionView
✅ Right-click MenuFlyout           ✅ SwipeView for actions
✅ Inline toolbars with icons       ✅ Floating Action Button (FAB)
✅ Side-by-side label+input forms   ✅ Stacked label-above-input forms
✅ Smaller tap targets OK           ✅ Min 48px tap targets ALWAYS
✅ Hover states matter              ✅ No hover — touch only
✅ Keyboard shortcuts (Ctrl+S etc.) ✅ Hardware back button support
✅ Resizable window layouts         ✅ Fixed portrait-first layouts
```

---

## 📚 Further Reading

- [MAUI Shell Navigation](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/shell/)
- [OnPlatform / OnIdiom](https://learn.microsoft.com/en-us/dotnet/maui/xaml/markup-extensions/consume#onidiom-markup-extension)
- [MVVM Community Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [MAUI Layouts Guide](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/layouts/)
- [CollectionView](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/collectionview/)
- [SwipeView](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/swipeview)
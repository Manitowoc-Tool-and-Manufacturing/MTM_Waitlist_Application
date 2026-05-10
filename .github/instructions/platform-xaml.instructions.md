---
applyTo: "**/*.csproj"
---

# Platform-Specific XAML Splitting

Every Feature project that has Windows AND Android XAML layouts must add the
following MSBuild ItemGroups to its `.csproj`. This prevents the MAUI source
generator from processing both files and only includes the file that matches
the current build target.

The canonical reference is `MTM_Waitlist_Application.Feature.Dashboard.csproj`.

## Required .csproj Pattern

Replace `<Screen>` and `<Feature>_<Screen>` with the actual screen name.

```xml
<!-- Platform-specific XAML: exclude both files from the default SDK glob so
     the MAUI source generator does not process them twice, then include only
     the file that matches the current build target. -->
<ItemGroup>
  <MauiXaml Remove="Views\<Screen>\View_<Feature>_<Screen>.Windows.xaml" />
  <MauiXaml Remove="Views\<Screen>\View_<Feature>_<Screen>.Android.xaml" />
  <None Remove="Views\<Screen>\View_<Feature>_<Screen>.Windows.xaml" />
  <None Remove="Views\<Screen>\View_<Feature>_<Screen>.Android.xaml" />
  <Content Include="Views\<Screen>\View_<Feature>_<Screen>.Windows.xaml">
    <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    <SubType>Designer</SubType>
    <Visible>true</Visible>
  </Content>
  <Content Include="Views\<Screen>\View_<Feature>_<Screen>.Android.xaml">
    <CopyToOutputDirectory>Never</CopyToOutputDirectory>
    <SubType>Designer</SubType>
    <Visible>true</Visible>
  </Content>
</ItemGroup>
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">
  <Content Remove="Views\<Screen>\View_<Feature>_<Screen>.Windows.xaml" />
  <MauiXaml Include="Views\<Screen>\View_<Feature>_<Screen>.Windows.xaml" />
</ItemGroup>
<ItemGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) != 'windows'">
  <Content Remove="Views\<Screen>\View_<Feature>_<Screen>.Android.xaml" />
  <MauiXaml Include="Views\<Screen>\View_<Feature>_<Screen>.Android.xaml" />
</ItemGroup>
```

## Full New Feature .csproj Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>
    <TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>

    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">21.0</SupportedOSPlatformVersion>
    <SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</SupportedOSPlatformVersion>
    <TargetPlatformMinVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">10.0.17763.0</TargetPlatformMinVersion>
    <!-- Suppress MVVMTK0045: partial property AOT warning is WinRT-specific
         and does not apply to MAUI WinUI. Safe to suppress. -->
    <NoWarn>$(NoWarn);MVVMTK0045</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.60" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Core\MTM_Waitlist_Application.Core\MTM_Waitlist_Application.Core.csproj" />
    <ProjectReference Include="..\..\Core\MTM_Waitlist_Application.Services\MTM_Waitlist_Application.Services.csproj" />
  </ItemGroup>

  <!-- Platform XAML ItemGroups go here — one block per screen -->

</Project>
```

## Rules
- The `.xaml.cs` code-behind is always a SINGLE shared file — never split per platform.
- The code-behind only sets `BindingContext = viewModel` — no platform-specific logic.
- `x:DataType` must be set on both XAML files to the same ViewModel type.
- Add one ItemGroup block per screen in the feature (repeat the pattern for each View).

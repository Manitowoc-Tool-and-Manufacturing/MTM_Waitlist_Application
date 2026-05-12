---
applyTo: "**/*.Tests.cs,**/*Tests/**"
---

# Testing Instructions — MTM Waitlist Application

## Test Projects

| Solution Folder | Project | Target Framework | References |
|---|---|---|---|
| `/Tests/Unit/` | `Core.Tests` | `net10.0` | `Core` |
| `/Tests/Unit/` | `Data.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Data` |
| `/Tests/Unit/` | `Services.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Services` |
| `/Tests/Unit/` | `Feature.Auth.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Feature.Auth` |
| `/Tests/Unit/` | `Feature.Dashboard.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Feature.Dashboard` |
| `/Tests/Unit/` | `Feature.Waitlist.Tests` | `net10.0-windows10.0.19041.0` | `Core`, `Feature.Waitlist` |
| `/Tests/UI/` | `MTM_Waitlist_Application.UITests.WinUI` | `net10.0-windows10.0.19041.0` | None (runtime Appium) |
| `/Tests/UI/` | `MTM_Waitlist_Application.UITests.Droid` | `net10.0` | None (runtime Appium) |

NuGet packages in every unit test project: `xunit.v3`, `xunit.runner.visualstudio`, `Moq`, `FluentAssertions`, `coverlet.collector`

---

## Folder Structure Rule

Test file paths mirror the source project path exactly, then add a **category subfolder** at the leaf level.

**Pattern:**
```
Tests/Unit/<TestProject>/<SourceFolder>/<SourceSubfolder>/<Category>/<TestFileName>.cs
```

**Category subfolder names** group tests within a leaf folder by concern:

| Category | Use for |
|---|---|
| `AuthSeeds` | Seeding / mock data related to authentication |
| `WaitlistSeeds` | Seeding / mock data related to waitlist entries |
| `Success` | Happy-path tests |
| `Failure` | Error / failure / offline path tests |
| `Validation` | Input validation and boundary tests |
| `Commands` | ViewModel `[RelayCommand]` method tests |
| `Properties` | ViewModel `[ObservableProperty]` state tests |
| `Connectivity` | Online vs. offline routing tests |

Add new category names as needed — keep them concise and consistent.

---

## Source → Test Path Mapping

### Core.Tests  (`net10.0`)
| Source file | Test file path |
|---|---|
| `Core/Core/Models/Auth/Model_AuthToken.cs` | `Tests/Unit/Core.Tests/Models/Auth/Success/Model_AuthTokenTests.cs` |
| `Core/Core/Models/Shared/Model_Dao_Result.cs` | `Tests/Unit/Core.Tests/Models/Shared/Success/Model_Dao_ResultTests.cs` |
| `Core/Core/Models/Waitlist/Model_WaitlistEntry.cs` | `Tests/Unit/Core.Tests/Models/Waitlist/Validation/Model_WaitlistEntryTests.cs` |

### Data.Tests  (`net10.0-windows10.0.19041.0`)
| Source file | Test file path |
|---|---|
| `Core/Data/Http/HttpApiClient.cs` | `Tests/Unit/Data.Tests/Http/Success/HttpApiClientTests.cs` |
| `Core/Data/Http/HttpApiClient.cs` | `Tests/Unit/Data.Tests/Http/Failure/HttpApiClientTests.cs` |
| `Core/Data/Local/LocalDbContext.cs` | `Tests/Unit/Data.Tests/Local/Success/LocalDbContextTests.cs` |
| `Core/Data/Mock/MockDataSeeder.cs` | `Tests/Unit/Data.Tests/Mock/AuthSeeds/MockDataSeederTests.cs` |
| `Core/Data/Mock/MockDataSeeder.cs` | `Tests/Unit/Data.Tests/Mock/WaitlistSeeds/MockDataSeederTests.cs` |
| `Core/Data/Repositories/Waitlist/Repository_WaitlistEntry.cs` | `Tests/Unit/Data.Tests/Repositories/Waitlist/Success/Repository_WaitlistEntryTests.cs` |
| `Core/Data/Repositories/Waitlist/Repository_WaitlistEntry.cs` | `Tests/Unit/Data.Tests/Repositories/Waitlist/Failure/Repository_WaitlistEntryTests.cs` |
| `Core/Data/Repositories/Waitlist/Repository_WaitlistEntryLocal.cs` | `Tests/Unit/Data.Tests/Repositories/Waitlist/Success/Repository_WaitlistEntryLocalTests.cs` |
| `Core/Data/Repositories/Waitlist/Repository_WaitlistEntryLocal.cs` | `Tests/Unit/Data.Tests/Repositories/Waitlist/Failure/Repository_WaitlistEntryLocalTests.cs` |

### Services.Tests  (`net10.0-windows10.0.19041.0`)
| Source file | Test file path |
|---|---|
| `Core/Services/Auth/Service_Auth.cs` | `Tests/Unit/Services.Tests/Auth/Success/Service_AuthTests.cs` |
| `Core/Services/Auth/Service_Auth.cs` | `Tests/Unit/Services.Tests/Auth/Failure/Service_AuthTests.cs` |
| `Core/Services/Sync/SyncService.cs` | `Tests/Unit/Services.Tests/Sync/Connectivity/SyncServiceTests.cs` |
| `Core/Services/Waitlist/Service_WaitlistEntry.cs` | `Tests/Unit/Services.Tests/Waitlist/Success/Service_WaitlistEntryTests.cs` |
| `Core/Services/Waitlist/Service_WaitlistEntry.cs` | `Tests/Unit/Services.Tests/Waitlist/Failure/Service_WaitlistEntryTests.cs` |
| `Core/Services/Waitlist/Service_WaitlistEntry.cs` | `Tests/Unit/Services.Tests/Waitlist/Connectivity/Service_WaitlistEntryTests.cs` |

### Feature.Auth.Tests  (`net10.0-windows10.0.19041.0`)
| Source file | Test file path |
|---|---|
| `Features/Feature.Auth/Feature.Auth/ViewModels/Login/ViewModel_Auth_Login.cs` | `Tests/Unit/Feature.Auth.Tests/Feature.Auth.Tests/ViewModels/Login/Properties/ViewModel_Auth_LoginTests.cs` |

### Feature.Dashboard.Tests  (`net10.0-windows10.0.19041.0`)
| Source file | Test file path |
|---|---|
| `Features/Feature.Dashboard/ViewModels/Main/ViewModel_Dashboard_Main.cs` | `Tests/Unit/Feature.Dashboard.Tests/ViewModels/Main/Commands/ViewModel_Dashboard_MainTests.cs` |
| `Features/Feature.Dashboard/ViewModels/Main/ViewModel_Dashboard_Main.cs` | `Tests/Unit/Feature.Dashboard.Tests/ViewModels/Main/Properties/ViewModel_Dashboard_MainTests.cs` |

### Feature.Waitlist.Tests  (`net10.0-windows10.0.19041.0`)
*(No source files yet — add entries as ViewModels and Views are created)*

---

## Framework
- **xUnit v3** test runner
- **Moq** for mocking interfaces
- **FluentAssertions** for readable assertions

## Test Naming Convention
`MethodName_Should<Result>_When<Condition>`

```csharp
// ✅ CORRECT
LoadEntriesAsync_ShouldPopulateEntries_WhenServiceReturnsSuccess()
ApproveEntryAsync_ShouldCallService_WhenEntryIsSelected()
GetAllEntriesAsync_ShouldReturnFailure_WhenDatabaseIsUnavailable()
```

---

## ViewModel Test Pattern

```csharp
namespace MTM_Waitlist_Application.Tests.Unit.Feature.Dashboard.ViewModels.Main.Commands;

/// <summary>
/// Tests for command methods on <see cref="ViewModel_Dashboard_Main"/>.
/// Uses mocked IService_WaitlistEntry — no database required.
/// </summary>
public class ViewModel_Dashboard_MainTests
{
    private readonly Mock<IService_WaitlistEntry> _mockService;
    private readonly ViewModel_Dashboard_Main _viewModel;

    public ViewModel_Dashboard_MainTests()
    {
        _mockService = new Mock<IService_WaitlistEntry>();
        _viewModel = new ViewModel_Dashboard_Main(_mockService.Object);
    }

    [Fact]
    public async Task LoadEntriesAsync_ShouldPopulateEntries_WhenServiceReturnsSuccess()
    {
        var expected = new List<Model_WaitlistEntry>
        {
            new() { OperatorName = "TEST-Operator" }
        };

        _mockService
            .Setup(s => s.GetAllEntriesAsync(default))
            .ReturnsAsync(Model_Dao_Result<List<Model_WaitlistEntry>>.Success(expected));

        await _viewModel.LoadEntriesCommand.ExecuteAsync(null);

        _viewModel.Entries.Should().HaveCount(1);
        _viewModel.Entries[0].OperatorName.Should().Be("TEST-Operator");
    }
}
```

---

## Service Test Pattern (connectivity-aware dual-repository)

```csharp
namespace MTM_Waitlist_Application.Tests.Unit.Services.Waitlist.Connectivity;

/// <summary>
/// Tests for online/offline routing in <see cref="Service_WaitlistEntry"/>.
/// </summary>
public class Service_WaitlistEntryTests
{
    private readonly Mock<IConnectivity> _mockConnectivity;
    private readonly Mock<IRepository_WaitlistEntry> _mockOnline;
    private readonly Mock<IRepository_WaitlistEntryLocal> _mockLocal;
    private readonly Service_WaitlistEntry _service;

    public Service_WaitlistEntryTests()
    {
        _mockConnectivity = new Mock<IConnectivity>();
        _mockOnline = new Mock<IRepository_WaitlistEntry>();
        _mockLocal = new Mock<IRepository_WaitlistEntryLocal>();
        _service = new Service_WaitlistEntry(
            _mockConnectivity.Object, _mockOnline.Object, _mockLocal.Object);
    }

    [Fact]
    public async Task GetAllEntriesAsync_ShouldUseLocalRepository_WhenOffline()
    {
        _mockConnectivity.Setup(c => c.NetworkAccess).Returns(NetworkAccess.None);
        var localData = new List<Model_WaitlistEntry>();
        _mockLocal
            .Setup(r => r.GetAllWaitlistEntriesAsync())
            .ReturnsAsync(Model_Dao_Result<List<Model_WaitlistEntry>>.Success(localData));

        var result = await _service.GetAllEntriesAsync();

        result.IsSuccess.Should().BeTrue();
        _mockOnline.Verify(r => r.GetAllWaitlistEntriesAsync(default), Times.Never);
    }
}
```

---

## Rules
- No Arrange/Act/Assert comments — code structure implies it
- No hardcoded magic strings — use `nameof()` or constants
- Test data prefixed with `"TEST-"` for easy identification
- One assertion concept per test — keep tests focused
- Use `IAsyncLifetime` for integration tests requiring setup/teardown
- Namespace must match the test file's folder path exactly
- One source file = one or more test files, split by category subfolder
- Never mix category concerns in one test file
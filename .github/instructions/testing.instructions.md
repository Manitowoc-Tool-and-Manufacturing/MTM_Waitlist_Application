---
applyTo: "**/*.Tests.cs,**/*Tests/**"
---

# Testing Instructions — MTM Waitlist Application

## Framework
- **xUnit** for test runner
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

## ViewModel Test Pattern

```csharp
namespace MTM_Waitlist_Application.Tests.Feature.Waitlist.ViewModels;

/// <summary>
/// Unit tests for <see cref="ViewModel_Waitlist_Entry"/>.
/// Uses mocked IService_WaitlistEntry — no database required.
/// </summary>
public class ViewModel_Waitlist_EntryTests
{
    private readonly Mock<IService_WaitlistEntry> _mockService;
    private readonly ViewModel_Waitlist_Entry _viewModel;

    public ViewModel_Waitlist_EntryTests()
    {
        _mockService = new Mock<IService_WaitlistEntry>();
        _viewModel = new ViewModel_Waitlist_Entry(_mockService.Object);
    }

    [Fact]
    public async Task LoadEntriesAsync_ShouldPopulateEntries_WhenServiceReturnsSuccess()
    {
        var expected = new List<Model_WaitlistEntry>
        {
            new() { Name = "Test Entry" }
        };

        _mockService
            .Setup(s => s.GetAllEntriesAsync())
            .ReturnsAsync(Model_Dao_Result<List<Model_WaitlistEntry>>.Success(expected));

        await _viewModel.LoadEntriesCommand.ExecuteAsync(null);

        _viewModel.Entries.Should().HaveCount(1);
        _viewModel.Entries[0].Name.Should().Be("Test Entry");
    }
}
```

## Rules
- No Arrange/Act/Assert comments — code structure implies it
- No hardcoded magic strings — use `nameof()` or constants
- Test data prefixed with `"TEST-"` for easy identification
- One assertion concept per test — keep tests focused
- Use `IAsyncLifetime` for integration tests requiring setup/teardown
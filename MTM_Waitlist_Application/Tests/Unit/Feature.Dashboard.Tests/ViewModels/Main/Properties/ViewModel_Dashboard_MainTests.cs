using FluentAssertions;
using Feature.Dashboard.ViewModels.Main;

namespace MTM_Waitlist_Application.Tests.Unit.Feature.Dashboard.ViewModels.Main.Properties;

public class ViewModel_Dashboard_MainTests
{
    [Fact]
    public void Constructor_ShouldDefaultIsBusyToFalse_WhenCreated()
    {
        var viewModel = new ViewModel_Dashboard_Main();

        viewModel.IsBusy.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldDefaultStatusMessageToReady_WhenCreated()
    {
        var viewModel = new ViewModel_Dashboard_Main();

        viewModel.StatusMessage.Should().Be("Ready");
    }

    [Fact]
    public void StatusMessage_ShouldRaisePropertyChanged_WhenValueChanges()
    {
        var viewModel = new ViewModel_Dashboard_Main();
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        viewModel.StatusMessage = "TEST-Updated";

        viewModel.StatusMessage.Should().Be("TEST-Updated");
        raisedProperties.Should().Contain(nameof(ViewModel_Dashboard_Main.StatusMessage));
    }

    [Fact]
    public void IsBusy_ShouldRaisePropertyChanged_WhenValueChanges()
    {
        var viewModel = new ViewModel_Dashboard_Main();
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

        viewModel.IsBusy = true;

        viewModel.IsBusy.Should().BeTrue();
        raisedProperties.Should().Contain(nameof(ViewModel_Dashboard_Main.IsBusy));
    }
}
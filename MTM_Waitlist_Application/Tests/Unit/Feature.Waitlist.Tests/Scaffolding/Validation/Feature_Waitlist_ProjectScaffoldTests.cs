using Feature.Waitlist.ViewModels.SetupTech;
using Feature.Waitlist.ViewModels.SetupTechConfirmation;
using Feature.Waitlist.ViewModels.SetupTechDunnage;
using Feature.Waitlist.ViewModels.SetupTechValidation;
using FluentAssertions;

namespace MTM_Waitlist_Application.Tests.Unit.Feature.Waitlist.Scaffolding.Validation;

public class Feature_Waitlist_ProjectScaffoldTests
{
    [Fact]
    public void SetupTechViewModels_ShouldExist_WhenFeatureWaitlistIsImplemented()
    {
        typeof(ViewModel_Waitlist_SetupTech).Should().NotBeNull();
        typeof(ViewModel_Waitlist_SetupTechValidation).Should().NotBeNull();
        typeof(ViewModel_Waitlist_SetupTechDunnage).Should().NotBeNull();
        typeof(ViewModel_Waitlist_SetupTechConfirmation).Should().NotBeNull();
    }
}
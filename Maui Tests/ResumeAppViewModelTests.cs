using Moq;
using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.ViewModels;

namespace Test.Unit;

public class ResumeAppViewModelTests
{
    private static Mock<IApiService> CreateApiService()
    {
        var api = new Mock<IApiService>();

        api.Setup(x => x.GetSkillsAsync()).ReturnsAsync(new List<SkillEntry>());
        api.Setup(x => x.GetEducationAsync()).ReturnsAsync(new List<EducationEntry>());
        api.Setup(x => x.GetExperienceAsync()).ReturnsAsync(new List<ExperienceEntry>());
        api.Setup(x => x.GetProjectsAsync()).ReturnsAsync(new List<ProjectEntry>());
        api.Setup(x => x.GetCertificationsAsync()).ReturnsAsync(new List<CertificationEntry>());

        return api;
    }

    private static Mock<ILocalStorageService> CreateLocalStorageService()
    {
        var storage = new Mock<ILocalStorageService>();

        storage.Setup(x => x.LoadSkillsDraftAsync()).ReturnsAsync(new List<SkillEntry>());
        storage.Setup(x => x.LoadEducationDraftAsync()).ReturnsAsync(new List<EducationEntry>());
        storage.Setup(x => x.LoadExperienceDraftAsync()).ReturnsAsync(new List<ExperienceEntry>());
        storage.Setup(x => x.LoadProjectsDraftAsync()).ReturnsAsync(new List<ProjectEntry>());
        storage.Setup(x => x.LoadCertificationsDraftAsync()).ReturnsAsync(new List<CertificationEntry>());
        storage.Setup(x => x.LoadProfileImagePathAsync()).ReturnsAsync((string?)null);
        storage.Setup(x => x.LoadProfileImageUrlAsync()).ReturnsAsync((string?)null);

        storage.Setup(x => x.SaveSkillsDraftAsync(It.IsAny<List<SkillEntry>>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.SaveEducationDraftAsync(It.IsAny<List<EducationEntry>>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.SaveExperienceDraftAsync(It.IsAny<List<ExperienceEntry>>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.SaveProjectsDraftAsync(It.IsAny<List<ProjectEntry>>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.SaveCertificationsDraftAsync(It.IsAny<List<CertificationEntry>>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.SaveProfileImagePathAsync(It.IsAny<string?>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.SaveProfileImageUrlAsync(It.IsAny<string?>())).Returns(Task.CompletedTask);
        storage.Setup(x => x.ClearSkillsDraftAsync()).Returns(Task.CompletedTask);
        storage.Setup(x => x.ClearEducationDraftAsync()).Returns(Task.CompletedTask);
        storage.Setup(x => x.ClearExperienceDraftAsync()).Returns(Task.CompletedTask);
        storage.Setup(x => x.ClearProjectsDraftAsync()).Returns(Task.CompletedTask);
        storage.Setup(x => x.ClearCertificationsDraftAsync()).Returns(Task.CompletedTask);

        return storage;
    }

    [Fact]
    public void SkillsViewModel_DefaultState_IsReadyForEditing()
    {
        var vm = new SkillsViewModel(CreateApiService().Object, CreateLocalStorageService().Object);

        Assert.Equal("Add skill", vm.SubmitButtonText);
        Assert.Equal("Intermediate", vm.SelectedProficiencyLevel);
        Assert.Equal("Programming Language", vm.SelectedCategory);
        Assert.False(vm.CanSave);
        Assert.Contains("Advanced", vm.ProficiencyLevels);
    }

    [Fact]
    public void SkillsViewModel_MarkDirtyCommand_SetsValidationState()
    {
        var vm = new SkillsViewModel(CreateApiService().Object, CreateLocalStorageService().Object);

        vm.MarkDirtyCommand.Execute(null);

        Assert.True(vm.HasUnsavedChanges);
        Assert.True(vm.HasFormValidation);
        Assert.Contains("Skill name is required.", vm.FormValidationMessage);
    }

    [Fact]
    public void EducationViewModel_MarkDirtyCommand_ShowsRequiredFieldMessages()
    {
        var vm = new EducationViewModel(CreateApiService().Object, CreateLocalStorageService().Object);

        vm.MarkDirtyCommand.Execute(null);

        Assert.True(vm.HasUnsavedChanges);
        Assert.True(vm.HasFormValidation);
        Assert.Contains("School / university is required.", vm.FormValidationMessage);
        Assert.Contains("Degree is required.", vm.FormValidationMessage);
        Assert.Contains("Field of study is required.", vm.FormValidationMessage);
    }

    [Fact]
    public void ExperienceViewModel_MarkDirtyCommand_ShowsRequiredFieldMessages()
    {
        var vm = new ExperienceViewModel(CreateApiService().Object, CreateLocalStorageService().Object);

        vm.MarkDirtyCommand.Execute(null);

        Assert.True(vm.HasUnsavedChanges);
        Assert.True(vm.HasFormValidation);
        Assert.Contains("Company is required.", vm.FormValidationMessage);
        Assert.Contains("Job title is required.", vm.FormValidationMessage);
        Assert.Contains("Description is required.", vm.FormValidationMessage);
    }

    [Fact]
    public void ProjectsViewModel_MarkDirtyCommand_ShowsRequiredFieldMessages()
    {
        var vm = new ProjectsViewModel(CreateApiService().Object, CreateLocalStorageService().Object);

        vm.MarkDirtyCommand.Execute(null);

        Assert.True(vm.HasUnsavedChanges);
        Assert.True(vm.HasFormValidation);
        Assert.Contains("Project name is required.", vm.FormValidationMessage);
        Assert.Contains("Project description is required.", vm.FormValidationMessage);
    }

    [Fact]
    public void CertificationsViewModel_MarkDirtyCommand_ShowsRequiredFieldMessages()
    {
        var vm = new CertificationsViewModel(CreateApiService().Object, CreateLocalStorageService().Object);

        vm.MarkDirtyCommand.Execute(null);

        Assert.True(vm.HasUnsavedChanges);
        Assert.True(vm.HasFormValidation);
        Assert.Contains("Certification name is required.", vm.FormValidationMessage);
        Assert.Contains("Issuing organization is required.", vm.FormValidationMessage);
    }
}

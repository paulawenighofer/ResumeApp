using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using ResumeApp.Views;
using System.Collections.ObjectModel;

namespace ResumeApp.ViewModels;

public partial class SkillsViewModel : ObservableObject
{
    private readonly IApiService _apiService;
    private readonly ILocalStorageService _localStorageService;

    [ObservableProperty]
    private string skillInput = string.Empty;

    [ObservableProperty]
    private string selectedProficiencyLevel = "Intermediate";

    [ObservableProperty]
    private string selectedCategory = "Programming Language";

    [ObservableProperty]
    private ObservableCollection<SkillEntry> skillEntries = [];

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private bool isEditing;

    private string? _editingSkillId;

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add skill";

    public IList<string> ProficiencyLevels { get; } = ["Beginner", "Intermediate", "Advanced", "Expert"];
    public IList<string> SkillCategories { get; } =
    [
        "Programming Language",
        "Framework",
        "Tool",
        "Database",
        "Cloud",
        "Leadership",
        "Soft Skills",
        "Other"
    ];

    public IList<string> SuggestedSkills { get; } =
    [
        "C#",
        ".NET",
        "SQL",
        "Azure",
        "Docker",
        "Git",
        "REST APIs",
        "Team Leadership"
    ];

    public SkillsViewModel(IApiService apiService, ILocalStorageService localStorageService)
    {
        _apiService = apiService;
        _localStorageService = localStorageService;
        _ = LoadDraftsAndEntriesAsync();
    }

    [RelayCommand]
    private async Task AddSkill()
    {
        ResetError();

        if (string.IsNullOrWhiteSpace(SkillInput))
        {
            ShowError("Please enter a skill name.");
            return;
        }

        if (SkillEntries.Any(s =>
                s.Name.Equals(SkillInput.Trim(), StringComparison.OrdinalIgnoreCase) &&
                s.Id != _editingSkillId))
        {
            ShowError("This skill has already been added.");
            return;
        }

        var skill = new SkillEntry
        {
            Id = _editingSkillId ?? Guid.NewGuid().ToString(),
            Name = SkillInput.Trim(),
            ProficiencyLevel = SelectedProficiencyLevel,
            Category = SelectedCategory
        };

        if (IsEditing && _editingSkillId is not null)
        {
            ReplaceSkill(skill);
        }
        else
        {
            SkillEntries.Add(skill);
        }

        await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());
        ResetEditor();
    }

    [RelayCommand]
    private void EditSkill(SkillEntry? skill)
    {
        if (skill is null)
        {
            return;
        }

        _editingSkillId = skill.Id;
        IsEditing = true;
        OnPropertyChanged(nameof(SubmitButtonText));
        SkillInput = skill.Name;
        SelectedProficiencyLevel = skill.ProficiencyLevel;
        SelectedCategory = skill.Category;
    }

    [RelayCommand]
    private void CancelEdit() => ResetEditor();

    [RelayCommand]
    private async Task AddSuggestedSkill(string skillName)
    {
        if (SkillEntries.Any(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        SkillEntries.Add(new SkillEntry
        {
            Id = Guid.NewGuid().ToString(),
            Name = skillName,
            ProficiencyLevel = SelectedProficiencyLevel,
            Category = SelectedCategory
        });

        await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());
    }

    [RelayCommand]
    private async Task DeleteSkill(SkillEntry? skill)
    {
        if (skill is null || !SkillEntries.Contains(skill))
        {
            return;
        }

        SkillEntries.Remove(skill);
        await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());
        if (_editingSkillId == skill.Id)
        {
            ResetEditor();
        }
    }

    [RelayCommand]
    private async Task SaveAndContinue()
    {
        try
        {
            IsBusy = true;
            ResetError();

            if (HasPendingSkillInput())
            {
                var added = await TryAddCurrentSkillAsDraftAsync();
                if (!added)
                {
                    return;
                }
            }

            if (SkillEntries.Count == 0)
            {
                ShowError("Please add at least one skill.");
                return;
            }

            await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());

            var syncFailed = false;

            foreach (var skill in SkillEntries)
            {
                var success = int.TryParse(skill.Id, out _)
                    ? await _apiService.UpdateSkillAsync(skill)
                    : await _apiService.PostSkillAsync(skill);

                if (!success)
                {
                    syncFailed = true;
                    break;
                }
            }

            if (!syncFailed)
            {
                await _localStorageService.ClearSkillsDraftAsync();
            }

            if (syncFailed)
            {
                ShowError("Skills were saved locally, but the backend sync failed. You can continue and sync again later.");
            }

            await Shell.Current.GoToAsync(nameof(ProjectsPage));
        }
        catch (Exception ex)
        {
            ShowError($"Error while saving: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadDraftsAndEntriesAsync()
    {
        var drafts = await _localStorageService.LoadSkillsDraftAsync();
        if (drafts.Count > 0)
        {
            SkillEntries = new ObservableCollection<SkillEntry>(drafts);
        }

        var entries = await _apiService.GetSkillsAsync();
        if (entries.Count > 0)
        {
            SkillEntries = new ObservableCollection<SkillEntry>(entries);
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ResetError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    partial void OnIsEditingChanged(bool value) => OnPropertyChanged(nameof(SubmitButtonText));

    private bool HasPendingSkillInput() => !string.IsNullOrWhiteSpace(SkillInput);

    private async Task<bool> TryAddCurrentSkillAsDraftAsync()
    {
        if (string.IsNullOrWhiteSpace(SkillInput))
        {
            ShowError("Please enter a skill name or clear the field before continuing.");
            return false;
        }

        await AddSkill();
        return !HasError;
    }

    private void ReplaceSkill(SkillEntry skill)
    {
        var index = SkillEntries
            .Select((item, idx) => new { item, idx })
            .FirstOrDefault(x => x.item.Id == skill.Id)?.idx ?? -1;

        if (index >= 0)
        {
            SkillEntries[index] = skill;
        }
    }

    private void ResetEditor()
    {
        _editingSkillId = null;
        IsEditing = false;
        OnPropertyChanged(nameof(SubmitButtonText));
        SkillInput = string.Empty;
        SelectedProficiencyLevel = "Intermediate";
        SelectedCategory = "Programming Language";
    }
}

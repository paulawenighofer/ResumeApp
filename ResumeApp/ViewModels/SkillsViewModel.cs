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

    private string _skillInput = string.Empty;
    private string _selectedProficiencyLevel = "Intermediate";
    private string _selectedCategory = "Programming Language";
    private ObservableCollection<SkillEntry> _skillEntries = [];
    private bool _isBusy;
    private string _errorMessage = string.Empty;
    private bool _hasError;
    private bool _isEditing;
    private bool _hasUnsavedChanges;

    private string? _editingSkillId;

    public string SkillInput
    {
        get => _skillInput;
        set
        {
            if (SetProperty(ref _skillInput, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string SelectedProficiencyLevel
    {
        get => _selectedProficiencyLevel;
        set
        {
            if (SetProperty(ref _selectedProficiencyLevel, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public ObservableCollection<SkillEntry> SkillEntries
    {
        get => _skillEntries;
        set => SetProperty(ref _skillEntries, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(SubmitButtonText));
            }
        }
    }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                OnPropertyChanged(nameof(CanSave));
            }
        }
    }

    public string SubmitButtonText => IsEditing ? "Save changes" : "Add skill";

    public bool CanSave => !IsBusy && (HasUnsavedChanges || HasPendingSkillInput());

    [RelayCommand]
    private void MarkDirty() => HasUnsavedChanges = true;

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

        HasUnsavedChanges = true;

        await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());
        if (!await SyncSkillAsync(skill))
        {
            ShowError("Skill saved locally. Backend sync failed — please try again.");
        }
        else
        {
            await ShowToastAsync("Skill saved");
        }
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
        ResetError();

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

        HasUnsavedChanges = true;

        await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());
        var addedSkill = SkillEntries.Last();
        if (!await SyncSkillAsync(addedSkill))
        {
            ShowError("Skill saved locally. Backend sync failed — please try again.");
        }
        else
        {
            await ShowToastAsync("Skill added");
        }
    }

    [RelayCommand]
    private async Task DeleteSkill(SkillEntry? skill)
    {
        if (skill is null || !SkillEntries.Contains(skill))
        {
            return;
        }

        if (!await ConfirmDeleteAsync("Delete skill", "Delete this skill?"))
        {
            return;
        }

        SkillEntries.Remove(skill);
        HasUnsavedChanges = true;
        await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());
        if (!await _apiService.DeleteSkillAsync(skill.Id))
        {
            ShowError("Skill removed locally. Backend delete failed — please try again.");
        }
        else
        {
            await ShowToastAsync("Skill deleted");
        }
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
                HasUnsavedChanges = false;
            }

            if (syncFailed)
            {
                ShowError("Skills saved locally. Backend sync failed — please try again.");
                return;
            }

            await Shell.Current.GoToAsync("..");
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
            HasUnsavedChanges = true;
        }

        var entries = await _apiService.GetSkillsAsync();
        if (entries.Count > 0)
        {
            SkillEntries = new ObservableCollection<SkillEntry>(entries);
            HasUnsavedChanges = false;
        }
    }

    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
        _ = ShowToastAsync(message, isError: true);
    }

    private void ResetError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    private async Task<bool> SyncSkillAsync(SkillEntry skill)
    {
        var success = int.TryParse(skill.Id, out _)
            ? await _apiService.UpdateSkillAsync(skill)
            : await _apiService.PostSkillAsync(skill);

        if (success)
        {
            await _localStorageService.SaveSkillsDraftAsync(SkillEntries.ToList());
        }

        return success;
    }

    private static Task ShowToastAsync(string message, bool isError = false)
        => ToastService.ShowAsync(message, isError);

    private static Task<bool> ConfirmDeleteAsync(string title, string message)
        => Shell.Current?.CurrentPage?.DisplayAlert(title, message, "Delete", "Cancel")
           ?? Task.FromResult(true);

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

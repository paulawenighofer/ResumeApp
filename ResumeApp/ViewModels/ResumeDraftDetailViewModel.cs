using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using Shared.Models;

namespace ResumeApp.ViewModels;

public partial class ResumeDraftDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IApiService _apiService;

    [ObservableProperty] private int draftId;
    [ObservableProperty] private string targetCompany = string.Empty;
    [ObservableProperty] private string statusText = "Generating";
    [ObservableProperty] private string statusColorHex = "#7C3AED";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasFailedReason;
    [ObservableProperty] private string failedReason = string.Empty;
    [ObservableProperty] private bool hasSections;

    public ObservableCollection<ResumePreviewSection> Sections { get; } = [];

    public ResumeDraftDetailViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var raw) &&
            int.TryParse(raw?.ToString(), out var id))
        {
            DraftId = id;
        }
    }

    [RelayCommand]
    private async Task Appearing()
    {
        if (DraftId > 0)
        {
            await LoadDraftAsync(DraftId);
        }
    }

    public async Task LoadDraftAsync(int id)
    {
        if (id <= 0 || IsBusy)
        {
            return;
        }

        DraftId = id;
        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var draft = await _apiService.GetResumeDraftAsync(id);
            if (draft is null)
            {
                HasError = true;
                ErrorMessage = "Draft not found.";
                return;
            }

            TargetCompany = draft.TargetCompany;
            UpdateStatus(draft.Status);

            FailedReason = draft.FailedReason ?? string.Empty;
            HasFailedReason = !string.IsNullOrWhiteSpace(FailedReason);

            BuildSections(draft.EditedResumeJson, draft.GeneratedResumeJson);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Could not load draft. {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateStatus(ResumeDraftStatus status)
    {
        StatusText = status switch
        {
            ResumeDraftStatus.Generated => "Ready",
            ResumeDraftStatus.Failed => "Failed",
            _ => "Generating"
        };

        StatusColorHex = status switch
        {
            ResumeDraftStatus.Generated => "#16A34A",
            ResumeDraftStatus.Failed => "#DC2626",
            _ => "#7C3AED"
        };
    }

    private void BuildSections(string? editedJson, string? generatedJson)
    {
        Sections.Clear();

        var json = !string.IsNullOrWhiteSpace(editedJson)
            ? editedJson
            : generatedJson;

        if (string.IsNullOrWhiteSpace(json))
        {
            HasSections = false;
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                HasSections = false;
                return;
            }

            AddUserSection(root);
            AddArraySection(root, "education", "Education");
            AddArraySection(root, "experience", "Experience");
            AddArraySection(root, "skills", "Skills");
            AddArraySection(root, "projects", "Projects");
            AddArraySection(root, "certifications", "Certifications");

            HasSections = Sections.Count > 0;
        }
        catch
        {
            HasSections = false;
        }
    }

    private void AddUserSection(JsonElement root)
    {
        if (!root.TryGetProperty("user", out var user) || user.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var items = new List<string>();
        foreach (var property in user.EnumerateObject())
        {
            var text = ToInlineText(property.Value);
            if (!string.IsNullOrWhiteSpace(text))
            {
                items.Add($"{ToTitle(property.Name)}: {text}");
            }
        }

        if (items.Count > 0)
        {
            Sections.Add(new ResumePreviewSection
            {
                Title = "Profile",
                Items = items
            });
        }
    }

    private void AddArraySection(JsonElement root, string propertyName, string title)
    {
        if (!root.TryGetProperty(propertyName, out var section) || section.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var items = new List<string>();
        foreach (var element in section.EnumerateArray())
        {
            var text = ToInlineText(element);
            if (!string.IsNullOrWhiteSpace(text))
            {
                items.Add(text);
            }
        }

        if (items.Count == 0)
        {
            items.Add("No data provided.");
        }

        Sections.Add(new ResumePreviewSection
        {
            Title = title,
            Items = items
        });
    }

    private static string ToInlineText(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => string.Join(", ", element.EnumerateArray().Select(ToInlineText).Where(x => !string.IsNullOrWhiteSpace(x))),
            JsonValueKind.Object => string.Join(" • ", element.EnumerateObject()
                .Select(p => new { Name = ToTitle(p.Name), Value = ToInlineText(p.Value) })
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{x.Name}: {x.Value}")),
            _ => string.Empty
        };
    }

    private static string ToTitle(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var chars = new List<char>(name.Length + 4)
        {
            char.ToUpperInvariant(name[0])
        };

        for (var i = 1; i < name.Length; i++)
        {
            var current = name[i];
            var previous = name[i - 1];

            if (char.IsUpper(current) && !char.IsWhiteSpace(previous))
            {
                chars.Add(' ');
            }

            chars.Add(current);
        }

        return new string(chars.ToArray());
    }
}

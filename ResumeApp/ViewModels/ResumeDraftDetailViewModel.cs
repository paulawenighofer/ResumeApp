using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ResumeApp.Models;
using ResumeApp.Services;
using Shared.DTO;
using Shared.Models;
using System.Threading;

namespace ResumeApp.ViewModels;

public partial class ResumeDraftDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IApiService _apiService;
    private ResumeDetailDto? _currentDraft;
    private string? _currentEditedJson;
    private CancellationTokenSource? _pollingCts;
    private const int PollIntervalSeconds = 3;
    private const int MaxPollAttempts = 120;

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
    [ObservableProperty] private bool canEdit;
    [ObservableProperty] private bool canApprove;
    [ObservableProperty] private bool canGeneratePdf;
    [ObservableProperty] private bool canOpenPdf;
    [ObservableProperty] private bool showPdfRetry;
    [ObservableProperty] private bool isApproved;
    [ObservableProperty] private string saveDraftStatusMessage = string.Empty;
    [ObservableProperty] private bool showSaveDraftStatus;

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
            StartPollingIfPending();
        }
    }

    [RelayCommand]
    private void Disappearing()
    {
        StopPolling();
    }

    public async Task LoadDraftAsync(int id, bool showBusy = true)
    {
        if (id <= 0 || (showBusy && IsBusy))
        {
            return;
        }

        DraftId = id;

        if (showBusy)
        {
            IsBusy = true;
            HasError = false;
            ErrorMessage = string.Empty;
        }

        try
        {
            var draft = await _apiService.GetResumeDraftAsync(id);
            if (draft is null)
            {
                HasError = true;
                ErrorMessage = "Draft not found.";
                return;
            }

            _currentDraft = draft;
            TargetCompany = draft.TargetCompany;
            UpdateStatus(draft.Status);

            FailedReason = ResolveFailureReason(draft);
            HasFailedReason = !string.IsNullOrWhiteSpace(FailedReason);

            // Store the edited JSON for later submission
            _currentEditedJson = draft.EditedResumeJson ?? draft.GeneratedResumeJson;

            BuildSections(draft.EditedResumeJson, draft.GeneratedResumeJson);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Could not load draft. {ex.Message}";
        }
        finally
        {
            if (showBusy)
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    public async Task SaveDraftEdit()
    {
        if (IsBusy || DraftId <= 0 || _currentEditedJson is null)
        {
            return;
        }

        IsBusy = true;
        ShowSaveDraftStatus = true;

        try
        {
            var request = new SaveDraftEditRequest
            {
                EditedResumeJson = _currentEditedJson
            };

            var updated = await _apiService.SaveResumeDraftEditAsync(DraftId, request);
            if (updated is null)
            {
                SaveDraftStatusMessage = "Failed to save draft edits.";
                return;
            }

            _currentDraft = updated;
            UpdateStatus(updated.Status);
            SaveDraftStatusMessage = "Draft edits saved successfully!";

            // Auto-hide success message after 2 seconds
            await Task.Delay(2000);
            ShowSaveDraftStatus = false;
        }
        catch (Exception ex)
        {
            SaveDraftStatusMessage = $"Error saving draft: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ApproveDraft()
    {
        if (IsBusy || DraftId <= 0 || _currentEditedJson is null)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Approve Resume",
            "Are you sure you want to approve this resume? It will be locked for PDF generation.",
            "Approve", "Cancel");

        if (!confirmed)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var request = new ApproveDraftRequest
            {
                FinalResumeJson = _currentEditedJson
            };

            var approved = await _apiService.ApproveResumeDraftAsync(DraftId, request);
            if (approved is null)
            {
                HasError = true;
                ErrorMessage = "Failed to approve draft.";
                return;
            }

            IsApproved = true;
            UpdateStatus(ResumeDraftStatus.Approved);
            await Shell.Current.DisplayAlert("Success", "Resume approved and locked for PDF generation!", "OK");

            // Reload to show updated state
            await LoadDraftAsync(DraftId);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error approving draft: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task GeneratePdf()
    {
        if (IsBusy || DraftId <= 0 || !CanGeneratePdf)
        {
            return;
        }

        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var updated = await _apiService.GenerateResumePdfAsync(DraftId);
            if (updated is null)
            {
                HasError = true;
                ErrorMessage = "Failed to generate PDF.";
                return;
            }

            _currentDraft = updated;
            FailedReason = ResolveFailureReason(updated);
            HasFailedReason = !string.IsNullOrWhiteSpace(FailedReason);
            UpdateStatus(updated.Status);

            if (updated.Status == ResumeDraftStatus.PdfReady)
            {
                await Shell.Current.DisplayAlert("Success", "PDF generated successfully.", "OK");
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error generating PDF: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task OpenPdf()
    {
        if (IsBusy || DraftId <= 0 || !CanOpenPdf)
        {
            return;
        }

        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            var bytes = await _apiService.DownloadResumePdfAsync(DraftId);
            if (bytes is null || bytes.Length == 0)
            {
                HasError = true;
                ErrorMessage = "PDF is not available yet.";
                return;
            }

            var filePath = Path.Combine(FileSystem.CacheDirectory, $"resume-{DraftId}.pdf");
            await File.WriteAllBytesAsync(filePath, bytes);

            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(filePath)
            });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Error opening PDF: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void UpdateEditedJson(string jsonContent)
    {
        _currentEditedJson = jsonContent;
    }

    private void UpdateStatus(ResumeDraftStatus status)
    {
        IsApproved = status is ResumeDraftStatus.Approved
            or ResumeDraftStatus.PdfGenerating
            or ResumeDraftStatus.PdfReady
            or ResumeDraftStatus.PdfFailed;

        CanEdit = status is ResumeDraftStatus.Generated or ResumeDraftStatus.DraftReady or ResumeDraftStatus.DraftFailed or ResumeDraftStatus.Failed;
        CanApprove = status is ResumeDraftStatus.Generated or ResumeDraftStatus.DraftReady;
        CanGeneratePdf = status is ResumeDraftStatus.Approved or ResumeDraftStatus.PdfFailed;
        CanOpenPdf = status == ResumeDraftStatus.PdfReady || (_currentDraft?.HasPdf ?? false);
        ShowPdfRetry = status == ResumeDraftStatus.PdfFailed;

        StatusText = status switch
        {
            ResumeDraftStatus.Generated => "Ready",
            ResumeDraftStatus.DraftReady => "Ready to Approve",
            ResumeDraftStatus.DraftFailed or ResumeDraftStatus.Failed => "Draft Failed",
            ResumeDraftStatus.Approved => "Approved",
            ResumeDraftStatus.PdfGenerating => "PDF Generating",
            ResumeDraftStatus.PdfReady => "PDF Ready",
            ResumeDraftStatus.PdfFailed => "PDF Failed",
            ResumeDraftStatus.Pending => "Generating",
            _ => "Generating"
        };

        StatusColorHex = status switch
        {
            ResumeDraftStatus.Generated => "#16A34A",
            ResumeDraftStatus.DraftReady => "#0284C7",
            ResumeDraftStatus.DraftFailed or ResumeDraftStatus.Failed => "#DC2626",
            ResumeDraftStatus.Approved => "#1E40AF",
            ResumeDraftStatus.PdfGenerating => "#7C3AED",
            ResumeDraftStatus.PdfReady => "#0F766E",
            ResumeDraftStatus.PdfFailed => "#DC2626",
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

    private static string ResolveFailureReason(ResumeDetailDto draft)
    {
        if (draft.Status == ResumeDraftStatus.PdfFailed)
        {
            return draft.PdfFailureReason ?? string.Empty;
        }

        return draft.FailedReason ?? string.Empty;
    }

    private void StartPollingIfPending()
    {
        if (_currentDraft?.Status != ResumeDraftStatus.Pending || DraftId <= 0 || _pollingCts is not null)
        {
            return;
        }

        _pollingCts = new CancellationTokenSource();
        _ = PollDraftUntilCompletedAsync(_pollingCts.Token);
    }

    private void StopPolling()
    {
        _pollingCts?.Cancel();
        _pollingCts?.Dispose();
        _pollingCts = null;
    }

    private async Task PollDraftUntilCompletedAsync(CancellationToken cancellationToken)
    {
        try
        {
            for (var attempt = 0; attempt < MaxPollAttempts && !cancellationToken.IsCancellationRequested; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), cancellationToken);

                await LoadDraftAsync(DraftId, showBusy: false);

                if (_currentDraft?.Status != ResumeDraftStatus.Pending)
                {
                    StopPolling();
                    return;
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                HasError = true;
                ErrorMessage = "Draft is still generating. Please reopen this page in a moment.";
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_currentDraft?.Status != ResumeDraftStatus.Pending)
            {
                StopPolling();
            }
        }
    }
}

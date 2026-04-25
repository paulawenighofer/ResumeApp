using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class ResumeDraftsTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private HttpClient _client = null!;

    public ResumeDraftsTests()
    {
        _factory = new ApiFactory();
    }

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();
        _client = _factory.CreateClient();
        _factory.EmailService.Reset();
        _factory.AiResumeGenerationClient.Reset();
        _factory.PdfRenderer.ShouldFail = false;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateDraft_ThenListAndGetById_ReturnsGeneratedDraft()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "draft_ok@example.com",
            firstName: "Draft",
            lastName: "User");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Software Engineer",
            targetCompany = "Contoso",
            jobDescription = "Build backend APIs",
            experienceLevel = "Mid-level",
            resumeFormat = "PDF",
            personalSummary = "C# developer",
            includeEducation = true,
            includeExperience = true,
            includeSkills = true,
            includeProjects = true,
            includeCertifications = true
        });

        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(1, created.GetProperty("status").GetInt32());

        var id = created.GetProperty("id").GetInt32();

        var listRes = await authed.GetAsync("api/resumes");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);

        var list = await listRes.Content.ReadFromJsonAsync<List<JsonElement>>(AuthTestHelpers.JsonOpts);
        Assert.NotNull(list);
        Assert.Single(list!);

        var detailRes = await authed.GetAsync($"api/resumes/{id}");
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);

        var detail = await detailRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.False(string.IsNullOrWhiteSpace(detail.GetProperty("generationRequestJson").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(detail.GetProperty("generatedResumeJson").GetString()));
    }

    [Fact]
    public async Task CreateDraft_WhenAiReturnsUnsupportedFields_SetsFailedStatus()
    {
        _factory.AiResumeGenerationClient.ResponseJson = "{\"unknownSection\":[]}";

        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "draft_fail@example.com",
            firstName: "Draft",
            lastName: "Fail");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Software Engineer",
            targetCompany = "Fabrikam",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(2, created.GetProperty("status").GetInt32());
        Assert.Contains("Unsupported top-level field", created.GetProperty("failedReason").GetString());
    }

    [Fact]
    public async Task SaveDraftEdit_UpdatesEditedResumeJson_AndChangesStatusToDraftReady()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "draft_edit@example.com",
            firstName: "Draft",
            lastName: "Edit");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Software Engineer",
            targetCompany = "EditCo",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        var editedJson = "{\"user\":{\"name\":\"Edited Name\"},\"skills\":[\"C#\",\"Azure\"]}";

        var editRes = await authed.PutAsJsonAsync($"api/resumes/{id}/draft", new
        {
            editedResumeJson = editedJson
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, editRes.StatusCode);

        var updated = await editRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(3, updated.GetProperty("status").GetInt32()); // DraftReady = 3
        Assert.Equal(editedJson, updated.GetProperty("editedResumeJson").GetString());
    }

    [Fact]
    public async Task ApproveDraft_FinalizesDraftAndLocks_ChangesStatusToApproved()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "draft_approve@example.com",
            firstName: "Draft",
            lastName: "Approve");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Senior Developer",
            targetCompany = "ApproveCo",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        var finalJson = "{\"user\":{\"name\":\"Final Name\"},\"experience\":[\"Developer at TechCorp\"]}";

        var approveRes = await authed.PostAsJsonAsync($"api/resumes/{id}/approve", new
        {
            finalResumeJson = finalJson
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, approveRes.StatusCode);

        var approved = await approveRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(4, approved.GetProperty("status").GetInt32()); // Approved = 4

        // Verify that draft can be retrieved and shows approved state
        var getRes = await authed.GetAsync($"api/resumes/{id}");
        var detail = await getRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(4, detail.GetProperty("status").GetInt32());
        Assert.Equal(finalJson, detail.GetProperty("approvedJson").GetString());
    }

    [Fact]
    public async Task ApproveDraft_ThenEditAgain_ReturnsConflictError()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "draft_locked@example.com",
            firstName: "Draft",
            lastName: "Locked");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Full Stack Developer",
            targetCompany = "LockCo",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        var finalJson = "{\"user\":{\"name\":\"Locked Name\"}}";

        var approveRes = await authed.PostAsJsonAsync($"api/resumes/{id}/approve", new
        {
            finalResumeJson = finalJson
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, approveRes.StatusCode);

        // Attempt to edit after approval should fail
        var editRes = await authed.PutAsJsonAsync($"api/resumes/{id}/draft", new
        {
            editedResumeJson = "{\"user\":{\"name\":\"Attempted Edit\"}}"
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, editRes.StatusCode);
    }

    [Fact]
    public async Task SaveDraftEdit_AsAnotherUser_ReturnsNotFound()
    {
        var jwt1 = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "user1@example.com",
            firstName: "User",
            lastName: "One");

        var jwt2 = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "user2@example.com",
            firstName: "User",
            lastName: "Two");

        using var client1 = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt1);
        using var client2 = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt2);

        var createRes = await client1.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Software Engineer",
            targetCompany = "PrivateCo",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        // User2 attempts to edit User1's draft
        var editRes = await client2.PutAsJsonAsync($"api/resumes/{id}/draft", new
        {
            editedResumeJson = "{\"user\":{\"name\":\"Hacker\"}}"
        });

        Assert.Equal(System.Net.HttpStatusCode.NotFound, editRes.StatusCode);
    }

    [Fact]
    public async Task ApproveUnapprovedDraft_SucceedsWithoutEdit()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "draft_approve_no_edit@example.com",
            firstName: "Draft",
            lastName: "ApproveNoEdit");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "QA Engineer",
            targetCompany = "TestCo",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        Assert.Equal(System.Net.HttpStatusCode.Created, createRes.StatusCode);

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        // Get the generated JSON to use for approval
        var getRes = await authed.GetAsync($"api/resumes/{id}");
        var detail = await getRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var generatedJson = detail.GetProperty("generatedResumeJson").GetString();

        var approveRes = await authed.PostAsJsonAsync($"api/resumes/{id}/approve", new
        {
            finalResumeJson = generatedJson
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, approveRes.StatusCode);

        var approved = await approveRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(4, approved.GetProperty("status").GetInt32()); // Approved = 4
    }

    [Fact]
    public async Task CreateDraft_RateLimiting_ExceedingLimit_Returns429()
    {
        using var factory = new ApiFactory(useProductionRateLimits: true);
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        factory.EmailService.Reset();
        factory.AiResumeGenerationClient.Reset();

        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            client,
            factory,
            email: "rate_limit@example.com",
            firstName: "Rate",
            lastName: "Limited");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(factory, jwt);

        // Create 10 drafts successfully (limit)
        for (int i = 0; i < 10; i++)
        {
            var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
            {
                jobTitle = $"Software Engineer {i}",
                targetCompany = $"Company{i}",
                includeEducation = false,
                includeExperience = false,
                includeSkills = false,
                includeProjects = false,
                includeCertifications = false
            });

            Assert.Equal(System.Net.HttpStatusCode.Created, createRes.StatusCode);
        }

        // The 11th attempt should be rate limited
        var exceededRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Excess Request",
            targetCompany = "ShouldFail",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, exceededRes.StatusCode);

        var errorResponse = await exceededRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
    }

    [Fact]
    public async Task GeneratePdf_ForApprovedResume_SetsPdfReady_AndAllowsOwnerDownload()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "pdf_owner@example.com",
            firstName: "Pdf",
            lastName: "Owner");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Platform Engineer",
            targetCompany = "PdfCo",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        var approveRes = await authed.PostAsJsonAsync($"api/resumes/{id}/approve", new
        {
            finalResumeJson = "{\"user\":{\"name\":\"PDF Owner\"},\"skills\":[\"C#\"]}"
        });

        Assert.Equal(HttpStatusCode.OK, approveRes.StatusCode);

        var generateRes = await authed.PostAsync($"api/resumes/{id}/generate-pdf", null);
        Assert.Equal(HttpStatusCode.OK, generateRes.StatusCode);

        var generated = await generateRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(6, generated.GetProperty("status").GetInt32()); // PdfReady = 6
        Assert.True(generated.GetProperty("hasPdf").GetBoolean());

        var pdfRes = await authed.GetAsync($"api/resumes/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdfRes.StatusCode);
        Assert.Equal("application/pdf", pdfRes.Content.Headers.ContentType?.MediaType);

        var pdfBytes = await pdfRes.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(pdfBytes);
    }

    [Fact]
    public async Task GetPdf_AsAnotherUser_ReturnsNotFound()
    {
        var jwtOwner = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "pdf_owner2@example.com",
            firstName: "Pdf",
            lastName: "OwnerTwo");

        var jwtOther = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "pdf_other@example.com",
            firstName: "Pdf",
            lastName: "Other");

        using var owner = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwtOwner);
        using var other = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwtOther);

        var createRes = await owner.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "Backend Engineer",
            targetCompany = "PrivatePdf",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        await owner.PostAsJsonAsync($"api/resumes/{id}/approve", new
        {
            finalResumeJson = "{\"user\":{\"name\":\"Private PDF\"}}"
        });

        await owner.PostAsync($"api/resumes/{id}/generate-pdf", null);

        var otherRes = await other.GetAsync($"api/resumes/{id}/pdf");
        Assert.Equal(HttpStatusCode.NotFound, otherRes.StatusCode);
    }

    [Fact]
    public async Task GeneratePdf_WhenRendererFails_MarksPdfFailed_AndRetryKeepsApprovedContent()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(
            _client,
            _factory,
            email: "pdf_retry@example.com",
            firstName: "Pdf",
            lastName: "Retry");

        using var authed = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);

        var createRes = await authed.PostAsJsonAsync("api/resumes/drafts", new
        {
            jobTitle = "DevOps Engineer",
            targetCompany = "RetryCo",
            includeEducation = false,
            includeExperience = false,
            includeSkills = false,
            includeProjects = false,
            includeCertifications = false
        });

        var created = await createRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        var id = created.GetProperty("id").GetInt32();

        var approvedJson = "{\"user\":{\"name\":\"Retry User\"},\"experience\":[\"Ops\"]}";

        await authed.PostAsJsonAsync($"api/resumes/{id}/approve", new
        {
            finalResumeJson = approvedJson
        });

        _factory.PdfRenderer.ShouldFail = true;

        var failedGenerateRes = await authed.PostAsync($"api/resumes/{id}/generate-pdf", null);
        Assert.Equal(HttpStatusCode.OK, failedGenerateRes.StatusCode);

        var failedGenerate = await failedGenerateRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(7, failedGenerate.GetProperty("status").GetInt32()); // PdfFailed = 7

        _factory.PdfRenderer.ShouldFail = false;

        var retryRes = await authed.PostAsync($"api/resumes/{id}/generate-pdf", null);
        Assert.Equal(HttpStatusCode.OK, retryRes.StatusCode);

        var detailRes = await authed.GetAsync($"api/resumes/{id}");
        var detail = await detailRes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);

        Assert.Equal(6, detail.GetProperty("status").GetInt32()); // PdfReady = 6
        Assert.Equal(approvedJson, detail.GetProperty("approvedJson").GetString());
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class ResumeDraftsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ResumeDraftsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        factory.EmailService.Reset();
        factory.AiResumeGenerationClient.Reset();
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
}

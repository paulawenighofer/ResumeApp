using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Test.Integration.Fixtures;

namespace Test.Integration;

public class ProfileAndResumeStorageTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ProfileAndResumeStorageTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Certification_And_Resume_Crud_Works_For_Authenticated_User()
    {
        var jwt = await AuthTestHelpers.RegisterAndVerifyAsync(_factory.CreateClient(), _factory,
            email: "profile_resume@example.com", password: "Password1");
        var client = AuthTestHelpers.CreateAuthenticatedClient(_factory, jwt);
        var certificationId = Guid.NewGuid().ToString("N");
        var resumeId = Guid.NewGuid().ToString("N");

        var createCert = await client.PostAsJsonAsync("tables/certifications", new
        {
            id = certificationId,
            name = "AWS Certified Cloud Practitioner",
            issuingOrganization = "Amazon Web Services",
            credentialId = "ABC-123"
        });
        Assert.Equal(HttpStatusCode.Created, createCert.StatusCode);
        var createdCert = await createCert.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(certificationId, createdCert.GetProperty("id").GetString());

        var createResume = await client.PostAsJsonAsync("tables/resumes", new
        {
            id = resumeId,
            targetJobTitle = "Software Engineer",
            targetCompany = "Example Corp",
            jobDescription = "Build APIs",
            companyDescription = "A product company",
            generatedContent = "{\"summary\":\"Test\"}"
        });
        Assert.Equal(HttpStatusCode.Created, createResume.StatusCode);
        var createdResume = await createResume.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.Equal(resumeId, createdResume.GetProperty("id").GetString());

        var listCerts = await client.GetAsync("tables/certifications");
        Assert.Equal(HttpStatusCode.OK, listCerts.StatusCode);
        var certs = await listCerts.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.True(certs.ValueKind == JsonValueKind.Array && certs.GetArrayLength() == 1);

        var listResumes = await client.GetAsync("tables/resumes");
        Assert.Equal(HttpStatusCode.OK, listResumes.StatusCode);
        var resumes = await listResumes.Content.ReadFromJsonAsync<JsonElement>(AuthTestHelpers.JsonOpts);
        Assert.True(resumes.ValueKind == JsonValueKind.Array && resumes.GetArrayLength() == 1);

        var deleteCert = await client.DeleteAsync($"tables/certifications/{certificationId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteCert.StatusCode);

        var deleteResume = await client.DeleteAsync($"tables/resumes/{resumeId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResume.StatusCode);
    }
}

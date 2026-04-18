using API.Services;

namespace Test.Integration.Fixtures;

public class FakeAiResumeGenerationClient : IAiResumeGenerationClient
{
    public string ResponseJson { get; set; } = "{\"user\":{},\"education\":[],\"experience\":[],\"skills\":[],\"projects\":[],\"certifications\":[]}";
    public Exception? ExceptionToThrow { get; set; }

    public Task<string> GenerateResumeJsonAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        return Task.FromResult(ResponseJson);
    }

    public void Reset()
    {
        ResponseJson = "{\"user\":{},\"education\":[],\"experience\":[],\"skills\":[],\"projects\":[],\"certifications\":[]}";
        ExceptionToThrow = null;
    }
}

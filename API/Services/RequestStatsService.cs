namespace API.Services;

public sealed class RequestStatsService
{
    private long _totalRequests;
    private long _failedRequests;

    public void Record(int statusCode)
    {
        Interlocked.Increment(ref _totalRequests);

        if (statusCode >= 400)
        {
            Interlocked.Increment(ref _failedRequests);
        }
    }

    public long TotalRequests => Interlocked.Read(ref _totalRequests);

    public long FailedRequests => Interlocked.Read(ref _failedRequests);
}

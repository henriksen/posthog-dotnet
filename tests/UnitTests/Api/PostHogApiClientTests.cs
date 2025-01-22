using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PostHog.Api;
using PostHog.Config;

public class PostHogApiClientTests
{
    public class TheCaptureBatchAsyncMethod
    {
        [Fact]
        public async Task SendsBatchToCaptureEndpoint()
        {
            using var messageHandler = new FakeHttpMessageHandler();
            var timeProvider = new FakeTimeProvider();
            timeProvider.SetUtcNow(new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero));
            var requestHandler = messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/batch"),
                HttpMethod.Post,
                responseBody: new { status = 1 });
            using var httpClient = new HttpClient(messageHandler);
            using var client = new PostHogApiClient(
                httpClient,
                new PostHogOptions { ProjectApiKey = "test" },
                timeProvider,
                new NullLogger<PostHogApiClient>());

            await client.CaptureBatchAsync([
                new CapturedEvent("some_event", "some-distinct-id", new Dictionary<string, object>(), timeProvider.GetUtcNow())
            ], CancellationToken.None);

            var received = requestHandler.GetReceivedRequestBody(indented: true);
            Assert.Equal($$"""
                         {
                           "historical_migrations": false,
                           "batch": [
                             {
                               "event": "some_event",
                               "distinct_id": "some-distinct-id",
                               "properties": {
                                 "$lib": "{{PostHogApiClient.LibraryName}}",
                                 "$lib_version": "{{client.Version}}"
                               },
                               "timestamp": "1999-12-31T16:00:00-08:00"
                             }
                           ],
                           "api_key": "test",
                           "timestamp": "1999-12-31T16:00:00-08:00"
                         }
                         """, received);
        }
    }
}
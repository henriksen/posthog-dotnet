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
            timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
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
                           "api_key": "test",
                           "historical_migrations": false,
                           "batch": [
                             {
                               "event": "some_event",
                               "properties": {
                                 "distinct_id": "some-distinct-id",
                                 "$lib": "posthog-dotnet",
                                 "$lib_version": "{{client.Version}}",
                                 "$geoip_disable": true
                               },
                               "timestamp": "2024-01-21T19:08:23\u002B00:00"
                             }
                           ]
                         }
                         """, received);
        }
    }
}
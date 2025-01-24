using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using PostHog.Api;
using UnitTests.Fakes;

namespace PostHogApiClientTests;

public class TheCaptureBatchAsyncMethod
{
    [Fact]
    public async Task SendsBatchToCaptureEndpoint()
    {
        var container = new FakeContainer();
        var messageHandler = container.GetRequiredService<FakeHttpMessageHandler>();
        var timeProvider = container.GetRequiredService<FakeTimeProvider>();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = messageHandler.AddResponse(
            new Uri("https://us.i.posthog.com/batch"),
            HttpMethod.Post,
            responseBody: new { status = 1 });
        var client = container.GetRequiredService<PostHogApiClient>();

        await client.CaptureBatchAsync([
            new CapturedEvent("some_event", "some-distinct-id", new Dictionary<string, object>(), timeProvider.GetUtcNow())
        ], CancellationToken.None);

        var received = requestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal($$"""
                     {
                       "api_key": "fake-project-api-key",
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

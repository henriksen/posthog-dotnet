using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using PostHog.Api;
using PostHog.Config;
using UnitTests.Fakes;

namespace PostHogApiClientTests;

public class TheCaptureBatchAsyncMethod
{
    [Fact]
    public async Task SendsBatchToCaptureEndpoint()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        var timeProvider = container.FakeTimeProvider;
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = messageHandler.AddBatchResponse();
        var client = container.Activate<PostHogApiClient>();

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

    [Fact]
    public async Task UsesAuthenticatedHttpClientForLocalEvaluationFlags()
    {
        var container = new TestContainer(services =>
        {
            services.Configure<PostHogOptions>(options =>
            {
                options.ProjectApiKey = "fake-project-api-key";
                options.PersonalApiKey = "fake-personal-api-key";
            });
        });
        var messageHandler = container.FakeHttpMessageHandler;
        var timeProvider = container.FakeTimeProvider;
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = messageHandler.AddLocalEvaluationResponse(
            responseBody: new LocalEvaluationApiResult(Flags: []));
        var client = container.Activate<PostHogApiClient>();

        await client.GetFeatureFlagsForLocalEvaluationAsync(cancellationToken: CancellationToken.None);

        var received = requestHandler.ReceivedRequest;
        Assert.NotNull(received.Headers.Authorization);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "fake-personal-api-key"), received.Headers.Authorization);
    }
}

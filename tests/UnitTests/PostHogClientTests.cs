using System.Net.Http.Headers;
using PostHog;
using PostHog.Versioning;
using UnitTests.Fakes;

#pragma warning disable CA2000
namespace PostHogClientTests;

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
        var client = container.Activate<PostHogClient>();

        var result = client.CaptureEvent("some-distinct-id", "some_event");

        Assert.True(result);
        await client.FlushAsync();
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
                               "$lib_version": "{{VersionConstants.Version}}",
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
        var container = new TestContainer("fake-personal-api-key");
        var timeProvider = container.FakeTimeProvider;
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            """
            {
              "flags":[
                 {
                    "id":1,
                    "name":"Beta Feature",
                    "key":"beta-feature",
                    "is_simple_flag":false,
                    "active":true,
                    "rollout_percentage":100,
                    "filters":{
                       "groups":[
                          {
                             "properties":[],
                             "rollout_percentage":100
                          }
                       ]
                    }
                 }
              ]
            }
            """
        );
        var client = container.Activate<PostHogClient>();

        await client.GetAllFeatureFlagsAsync("some-distinct-id");

        var received = requestHandler.ReceivedRequest;
        Assert.NotNull(received.Headers.Authorization);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", "fake-personal-api-key"), received.Headers.Authorization);
    }
}

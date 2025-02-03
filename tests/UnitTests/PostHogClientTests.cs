using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PostHog;
using PostHog.Config;
using PostHog.Versioning;
using UnitTests.Fakes;

#pragma warning disable CA2000
namespace PostHogClientTests;

public class TheCaptureEventMethod
{
    [Fact]
    public async Task SendsBatchToCaptureEndpoint()
    {
        var container = new TestContainer();
        container.FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = container.FakeHttpMessageHandler.AddBatchResponse();
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

    [Fact] // Ported from PostHog/posthog-python test_groups_capture
    public async Task CapturesGroups()
    {
        var container = new TestContainer();
        container.FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = container.FakeHttpMessageHandler.AddBatchResponse();
        var client = container.Activate<PostHogClient>();

        var result = client.CaptureEvent(
            distinctId: "some-distinct-id",
            eventName: "some_event",
            groups: [
                new Group("company", "id:5"),
                new Group("department", "engineering")
            ]);

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
                               "$geoip_disable": true,
                               "$groups": {
                                 "company": "id:5",
                                 "department": "engineering"
                               }
                             },
                             "timestamp": "2024-01-21T19:08:23\u002B00:00"
                           }
                         ]
                       }
                       """, received);
    }

    [Fact] // Ported from PostHog/posthog-python test_basic_super_properties
    public async Task SendsSuperPropertiesToEndpoint()
    {
        var container = new TestContainer(sp =>
        {
            sp.AddSingleton<IOptions<PostHogOptions>>(new PostHogOptions
            {
                ProjectApiKey = "fake-project-api-key",
                SuperProperties = new Dictionary<string, object> { ["source"] = "repo-name" }
            });
        });
        container.FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = container.FakeHttpMessageHandler.AddBatchResponse();
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
                               "$geoip_disable": true,
                               "source": "repo-name"
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
        container.FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
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

public class TheIdentifyPersonAsyncMethod
{
    [Fact]
    public async Task SendsCorrectPayload()
    {
        var container = new TestContainer();
        container.FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = container.FakeHttpMessageHandler.AddCaptureResponse();
        var client = container.Activate<PostHogClient>();

        var result = await client.IdentifyPersonAsync("some-distinct-id");

        Assert.Equal(1, result.Status);
        var received = requestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal($$"""
                       {
                         "event": "$identify",
                         "distinct_id": "some-distinct-id",
                         "properties": {
                           "$lib": "posthog-dotnet",
                           "$lib_version": "{{VersionConstants.Version}}",
                           "$geoip_disable": true
                         },
                         "api_key": "fake-project-api-key",
                         "timestamp": "2024-01-21T19:08:23\u002B00:00"
                       }
                       """, received);
    }

    [Fact]
    public async Task SendsCorrectPayloadWithPersonProperties()
    {
        var container = new TestContainer();
        container.FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = container.FakeHttpMessageHandler.AddCaptureResponse();
        var client = container.Activate<PostHogClient>();

        var result = await client.IdentifyPersonAsync(
            distinctId: "some-distinct-id",
            email: "wildling-lover@example.com",
            name: "Jon Snow",
            personPropertiesToSet: new() { ["age"] = 36 },
            personPropertiesToSetOnce: new() { ["join_date"] = "2024-01-21" },
            CancellationToken.None);

        Assert.Equal(1, result.Status);
        var received = requestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal($$"""
                       {
                         "event": "$identify",
                         "distinct_id": "some-distinct-id",
                         "properties": {
                           "$set": {
                             "age": 36,
                             "email": "wildling-lover@example.com",
                             "name": "Jon Snow"
                           },
                           "$set_once": {
                             "join_date": "2024-01-21"
                           },
                           "$lib": "posthog-dotnet",
                           "$lib_version": "{{VersionConstants.Version}}",
                           "$geoip_disable": true
                         },
                         "api_key": "fake-project-api-key",
                         "timestamp": "2024-01-21T19:08:23\u002B00:00"
                       }
                       """, received);
    }

    [Fact] // Ported from PostHog/posthog-python test_basic_super_properties
    public async Task SendsCorrectPayloadWithSuperProperties()
    {
        var container = new TestContainer(sp =>
        {
            sp.AddSingleton<IOptions<PostHogOptions>>(new PostHogOptions
            {
                ProjectApiKey = "fake-project-api-key",
                SuperProperties = new Dictionary<string, object> { ["source"] = "repo-name" }
            });
        });
        container.FakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
        var requestHandler = container.FakeHttpMessageHandler.AddCaptureResponse();
        var client = container.Activate<PostHogClient>();

        var result = await client.IdentifyPersonAsync("some-distinct-id");

        Assert.Equal(1, result.Status);
        var received = requestHandler.GetReceivedRequestBody(indented: true);
        Assert.Equal($$"""
                       {
                         "event": "$identify",
                         "distinct_id": "some-distinct-id",
                         "properties": {
                           "$lib": "posthog-dotnet",
                           "$lib_version": "{{VersionConstants.Version}}",
                           "$geoip_disable": true,
                           "source": "repo-name"
                         },
                         "api_key": "fake-project-api-key",
                         "timestamp": "2024-01-21T19:08:23\u002B00:00"
                       }
                       """, received);
    }
}

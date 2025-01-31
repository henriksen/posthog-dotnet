using Microsoft.Extensions.DependencyInjection;
using PostHog.Api;
using PostHog.Config;
using PostHog.Features;
using UnitTests.Fakes;

namespace LocalFeatureFlagsLoaderTests;

public class TheGetFeatureFlagsForLocalEvaluationAsyncMethod
{
    [Fact]
    public async Task RetrievesFeatureFlagsFromApi()
    {
        var container = new TestContainer(personalApiKey: "fake-personal-api-key");
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            responseBody: new LocalEvaluationApiResult(
                Flags: [
                    new LocalFeatureFlag(
                        Id: 123,
                        TeamId: 456,
                        Name: "Flag Name",
                        Key: "flag-key",
                        Filters: null)
                ],
                GroupTypeMapping: new Dictionary<string, string>()));
        var loader = container.Activate<LocalFeatureFlagsLoader>();

        var result = await loader.GetFeatureFlagsForLocalEvaluationAsync(CancellationToken.None);

        Assert.NotNull(result?.LocalEvaluationApiResult.Flags);
        var flag = Assert.Single(result.LocalEvaluationApiResult.Flags);
        Assert.Equal(new LocalFeatureFlag(
            Id: 123,
            TeamId: 456,
            Name: "Flag Name",
            Key: "flag-key",
            Filters: null), flag);
    }

    [Fact]
    public async Task UpdatesFeatureFlagsOnTimer()
    {
        var container = new TestContainer(services =>
        {
            services.Configure<PostHogOptions>(options =>
            {
                options.ProjectApiKey = "fake-project-api-key";
                options.PersonalApiKey = "fake-personal-api-key";
                options.FeatureFlagPollInterval = TimeSpan.FromSeconds(30);
            });
        });
        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            responseBody: new LocalEvaluationApiResult(
                Flags: [
                    new LocalFeatureFlag(
                        Id: 123,
                        TeamId: 456,
                        Name: "Flag Name",
                        Key: "flag-key",
                        Filters: null)
                ],
                GroupTypeMapping: new Dictionary<string, string>()));
        var loader = container.Activate<LocalFeatureFlagsLoader>();

        var result = await loader.GetFeatureFlagsForLocalEvaluationAsync(CancellationToken.None);

        Assert.NotNull(result?.LocalEvaluationApiResult.Flags);
        var flag = Assert.Single(result.LocalEvaluationApiResult.Flags);
        Assert.Equal(new LocalFeatureFlag(
            Id: 123,
            TeamId: 456,
            Name: "Flag Name",
            Key: "flag-key",
            Filters: null), flag);

        container.FakeHttpMessageHandler.AddLocalEvaluationResponse(
            responseBody: new LocalEvaluationApiResult(
                Flags: [
                    new LocalFeatureFlag(
                        Id: 123,
                        TeamId: 456,
                        Name: "Flag Name",
                        Key: "flag-key-2",
                        Filters: null)
                ],
                GroupTypeMapping: new Dictionary<string, string>()));
        container.FakeTimeProvider.Advance(TimeSpan.FromSeconds(31));
        await Task.Delay(1); // Cede execution to thread that's loading the new flags.

        var newResult = await loader.GetFeatureFlagsForLocalEvaluationAsync(CancellationToken.None);

        Assert.NotNull(newResult?.LocalEvaluationApiResult.Flags);
        var newFlag = Assert.Single(newResult.LocalEvaluationApiResult.Flags);
        Assert.Equal(new LocalFeatureFlag(
            Id: 123,
            TeamId: 456,
            Name: "Flag Name",
            Key: "flag-key-2",
            Filters: null), newFlag);

    }
}
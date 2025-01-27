using PostHog.Api;
using PostHog.Features;
using UnitTests.Fakes;

namespace LocalFeatureFlagsLoaderTests;

public class TheGetFeatureFlagsForLocalEvaluationAsyncMethod
{
    [Fact]
    public async Task RetrievesFeatureFlagsFromApi()
    {
        var container = new TestContainer();
        var messageHandler = container.FakeHttpMessageHandler;
        messageHandler.AddResponse(new Uri("https://us.i.posthog.com/api/feature_flag/local_evaluation/?token=fake-project-api-key?send_cohorts"),
            HttpMethod.Get,
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

        Assert.NotNull(result);
        var flag = Assert.Single(result.Flags);
        Assert.Equal(new LocalFeatureFlag(
            Id: 123,
            TeamId: 456,
            Name: "Flag Name",
            Key: "flag-key",
            Filters: null), flag);
    }
}
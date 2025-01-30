using PostHog.Api;
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
}
using PostHog.Json;
using PostHog.Models;

public class JsonSerializerHelperTests
{
    public class TheSerializeToCamelCaseJsonMethod
    {
        [Fact]
        public async Task ShouldSerializeObjectToCamelCaseJson()
        {
            // Arrange
            var obj = new { PropertyOne = "value", PropertyTwo = 1 };

            // Act
            var json = await JsonSerializerHelper.SerializeToCamelCaseJsonStringAsync(obj);

            // Assert
            Assert.Equal("{\"propertyOne\":\"value\",\"propertyTwo\":1}", json);
        }
    }

    public class TheDeserializeFromCamelCaseJsonMethod
    {
        [Fact]
        public async Task ShouldDeserializeFeatureFlagsJsonToFeatureFlagResult()
        {
            // Arrange
            var json = await File.ReadAllTextAsync("./Json/feature-flags-v3.json");

            // Act

            var result = await JsonSerializerHelper.DeserializeFromCamelCaseJson<FeatureFlagsResult>(json);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Config.EnableCollectEverything);
            Assert.Empty(result.ToolbarParams);
            Assert.False(result.IsAuthenticated);
            Assert.Equal(["gzip", "gzip-js"], result.SupportedCompression);
            Assert.Equal(new Dictionary<string, bool>
            {
                ["hogtied-homepage-user"] = true,
                ["hogtied-homepage-bonanza"] = true
            }, result.FeatureFlags);
            Assert.False(result.SessionRecording);
            Assert.False(result.CaptureDeadClicks);
            Assert.True(result.CapturePerformance.NetworkTiming);
            Assert.True(result.CapturePerformance.WebVitals);
            Assert.Null(result.CapturePerformance.WebVitalsAllowedMetrics);
            Assert.False(result.AutocaptureOptOut);
            Assert.False(result.AutocaptureExceptions);
            Assert.Equal("/i/v0/e/", result.Analytics.Endpoint);
            Assert.True(result.ElementsChainAsString);
            Assert.False(result.Surveys);
            Assert.True(result.Heatmaps);
            Assert.True(result.DefaultIdentifiedOnly);
            Assert.Empty(result.SiteApps);
            Assert.False(result.ErrorsWhileComputingFlags);
            Assert.Equal(new Dictionary<string, string>
            {
                ["hogtied-homepage-user"] = "{\"is_cool\": true}"
            }, result.FeatureFlagPayloads);
        }

        [Fact]
        public async Task ShouldDeserializeFeatureFlagsNegatedJsonToFeatureFlagResult()
        {
            // Arrange
            var json = await File.ReadAllTextAsync("./Json/feature-flags-v3-negated.json");

            // Act

            var result = await JsonSerializerHelper.DeserializeFromCamelCaseJson<FeatureFlagsResult>(json);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Config.EnableCollectEverything);
            Assert.Empty(result.ToolbarParams);
            Assert.True(result.IsAuthenticated);
            Assert.Equal(["gzip", "gzip-js"], result.SupportedCompression);
            Assert.Equal(new Dictionary<string, bool>
            {
                ["hogtied-homepage-user"] = false,
                ["hogtied-homepage-bonanza"] = false
            }, result.FeatureFlags);
            Assert.True(result.SessionRecording);
            Assert.True(result.CaptureDeadClicks);
            Assert.False(result.CapturePerformance.NetworkTiming);
            Assert.False(result.CapturePerformance.WebVitals);
            Assert.NotNull(result.CapturePerformance.WebVitalsAllowedMetrics);
            Assert.True(result.AutocaptureOptOut);
            Assert.True(result.AutocaptureExceptions);
            Assert.Equal("/i/v0/e/", result.Analytics.Endpoint);
            Assert.False(result.ElementsChainAsString);
            Assert.True(result.Surveys);
            Assert.False(result.Heatmaps);
            Assert.False(result.DefaultIdentifiedOnly);
            Assert.Empty(result.SiteApps);
            Assert.True(result.ErrorsWhileComputingFlags);
            Assert.Empty(result.FeatureFlagPayloads);
        }

        [Fact]
        public async Task ShouldDeserializeApiResult()
        {
            // Arrange
            var json = "{\"status\": 1}";

            // Act
            var result = await JsonSerializerHelper.DeserializeFromCamelCaseJson<ApiResult>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Status);
        }
    }
}
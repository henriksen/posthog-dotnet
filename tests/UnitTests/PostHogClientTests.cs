using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using PostHog;
using PostHog.Api;
using PostHog.Cache;
using PostHog.Config;
using PostHog.Json;

#pragma warning disable CA2000
public class PostHogClientTests
{
    public class TheGetFeatureFlagAsyncMethod
    {
        static PostHogClient SetUpPostHogClient(out FakeHttpMessageHandler messageHandler)
        {
            messageHandler = new FakeHttpMessageHandler();
            var timeProvider = new FakeTimeProvider();
            timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 21, 19, 08, 23, TimeSpan.Zero));
            var httpClient = new HttpClient(messageHandler);
            var options = new PostHogOptions { ProjectApiKey = "test" };
            var apiClient = new PostHogApiClient(
                httpClient,
                options,
                timeProvider,
                new NullLogger<PostHogApiClient>());
            return new PostHogClient(
                apiClient,
                NullFeatureFlagCache.Instance,
                options,
                timeProvider,
                new NullLogger<PostHogClient>());
        }

        [Fact]
        public async Task ReturnsUndefinedWhenFlagDoesNotExist()
        {
            await using var client = SetUpPostHogClient(out var messageHandler);
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = true
                    }.AsReadOnly()
                });

            var result = await client.GetFeatureFlagAsync(
                "distinctId",
                "unknown-flag-key",
                CancellationToken.None);

            Assert.Null(result);
            Assert.Equal("undefined", result.ToResponseObject());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReturnsFlag(bool enabled)
        {
            await using var client = SetUpPostHogClient(out var messageHandler);
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = enabled
                    }.AsReadOnly()
                });

            var result = await client.GetFeatureFlagAsync(
                "distinct-id",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal(enabled, result.IsEnabled);
        }

        [Fact]
        public async Task ReturnsStringFlag()
        {
            await using var client = SetUpPostHogClient(out var messageHandler);
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = "premium-experience"
                    }.AsReadOnly()
                });

            var result = await client.GetFeatureFlagAsync(
                "distinct-id",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("premium-experience", result.VariantKey);
        }

        [Fact]
        public async Task CapturesFeatureFlagCalledEvent()
        {
            await using var client = SetUpPostHogClient(out var messageHandler);
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = true
                    }.AsReadOnly()
                });
            var captureRequestHandler = messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/batch"),
                HttpMethod.Post,
                responseBody: new { status = 1 });

            var result = await client.GetFeatureFlagAsync(
                "a-distinct-id",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.True(result.IsEnabled);
            await client.FlushAsync();
            var received = captureRequestHandler.GetReceivedRequestBody(indented: true);
            Assert.Equal($$"""
                           {
                             "api_key": "test",
                             "historical_migrations": false,
                             "batch": [
                               {
                                 "event": "$feature_flag_called",
                                 "properties": {
                                   "$feature_flag": "flag-key",
                                   "$feature_flag_response": true,
                                   "locally_evaluated": false,
                                   "$feature/flag-key": true,
                                   "distinct_id": "a-distinct-id",
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
        public async Task CapturesFeatureFlagCalledEventOnlyOncePerDistinctIdAndFlagKey()
        {
            var client = SetUpPostHogClient(out var messageHandler);
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = "premium-experience"
                    }.AsReadOnly()
                });
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = "premium-experience"
                    }.AsReadOnly()
                });
            messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/decide?v=3"),
                HttpMethod.Post,
                responseBody: new FeatureFlagsApiResult
                {
                    FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                    {
                        ["flag-key"] = false
                    }.AsReadOnly()
                });
            var captureRequestHandler = messageHandler.AddResponse(
                new Uri("https://us.i.posthog.com/batch"),
                HttpMethod.Post,
                responseBody: new { status = 1 });

            await client.GetFeatureFlagAsync(
                "a-distinct-id",
                "flag-key",
                CancellationToken.None);
            var result = await client.GetFeatureFlagAsync(
                "a-distinct-id",
                "flag-key",
                CancellationToken.None);
            await client.GetFeatureFlagAsync(
                "another-distinct-id",
                "flag-key",
                CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("premium-experience", result.VariantKey);
            await client.FlushAsync();
            var received = captureRequestHandler.GetReceivedRequestBody(indented: true);
            Assert.Equal($$"""
                           {
                             "api_key": "test",
                             "historical_migrations": false,
                             "batch": [
                               {
                                 "event": "$feature_flag_called",
                                 "properties": {
                                   "$feature_flag": "flag-key",
                                   "$feature_flag_response": "premium-experience",
                                   "locally_evaluated": false,
                                   "$feature/flag-key": "premium-experience",
                                   "distinct_id": "a-distinct-id",
                                   "$lib": "posthog-dotnet",
                                   "$lib_version": "{{client.Version}}",
                                   "$geoip_disable": true
                                 },
                                 "timestamp": "2024-01-21T19:08:23\u002B00:00"
                               },
                               {
                                 "event": "$feature_flag_called",
                                 "properties": {
                                   "$feature_flag": "flag-key",
                                   "$feature_flag_response": false,
                                   "locally_evaluated": false,
                                   "$feature/flag-key": false,
                                   "distinct_id": "another-distinct-id",
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

    public class TheGetFeatureFlagsAsyncMethod
    {
        [Fact]
        public async Task RetrievesFlagFromHttpContextCacheOnSecondCall()
        {
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            var httpContext = new DefaultHttpContext();
            httpContextAccessor.HttpContext.Returns(httpContext);
            var cache = new HttpContextFeatureFlagCache(httpContextAccessor);
            var options = new FakeOptions<PostHogOptions>(new PostHogOptions());
            var apiClient = Substitute.For<IPostHogApiClient>();
            // Set up a call to the API client that returns all feature flags
            apiClient.GetFeatureFlagsAsync(
                    distinctUserId: "1234",
                    personProperties: null,
                    groupProperties: null,
                    cancellationToken: Arg.Any<CancellationToken>())
                .Returns(
                    _ => new FeatureFlagsApiResult
                    {
                        FeatureFlags = new Dictionary<string, StringOrValue<bool>>
                        {
                            ["flag-key"] = true,
                            ["another-flag-key"] = "some-value",
                        }.AsReadOnly()
                    },
                    _ => throw new InvalidOperationException("Called more than once")
                );

            await using var client = new PostHogClient(
                apiClient,
                cache,
                options,
                new FakeTimeProvider(),
                NullLogger<PostHogClient>.Instance);

            var flags = await client.GetFeatureFlagsAsync(
                distinctId: "1234",
                personProperties: null,
                groupProperties: null,
                CancellationToken.None);
            var flagsAgain = await client.GetFeatureFlagsAsync(
                distinctId: "1234",
                personProperties: null,
                groupProperties: null,
                CancellationToken.None);
            var firstFlag = await client.GetFeatureFlagAsync("1234", "flag-key");

            Assert.NotEmpty(flags);
            Assert.Same(flags, flagsAgain);
            Assert.NotNull(firstFlag);
            Assert.Equal("flag-key", firstFlag.Key);
        }
    }
}
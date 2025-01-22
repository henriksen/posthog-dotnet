using Microsoft.Extensions.Internal;

namespace PostHog.Library;

/// <summary>
/// An implementation of <see cref="ISystemClock"/> that uses a
/// <see cref="TimeProvider"/> under the hood.
/// </summary>
public class TimeProviderSystemClock(TimeProvider timeProvider) : ISystemClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
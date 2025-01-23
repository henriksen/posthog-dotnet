using System.Runtime.CompilerServices;

namespace PostHog.Library;

public static class Ensure
{
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))]string? name = null)
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }

        return value;
    }
}
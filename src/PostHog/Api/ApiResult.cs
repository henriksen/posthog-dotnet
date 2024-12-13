namespace PostHog.Models;

/// <summary>
/// Result of a PostHog API call.
/// </summary>
public class ApiResult(int status)
{
    /// <summary>
    /// The status.
    /// </summary>
    public int Status => status;
}
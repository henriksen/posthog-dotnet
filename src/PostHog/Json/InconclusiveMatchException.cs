namespace PostHog.Json;

public class InconclusiveMatchException : Exception
{
    public InconclusiveMatchException(string message) : base(message)
    {
    }

    public InconclusiveMatchException()
    {
    }

    public InconclusiveMatchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
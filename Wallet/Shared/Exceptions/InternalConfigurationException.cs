namespace Shared.Exceptions;

public class InternalConfigurationException : Exception
{
    public InternalConfigurationException()
        : base("An internal configuration error occurred.")
    {
    }

    public InternalConfigurationException(string message)
        : base(message)
    {
    }

    public InternalConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
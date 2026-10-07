using System.Net;

namespace BankSimulator.Application;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }

    protected Result(bool success, T value, string error)
    {
        IsSuccess = success;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, default);

    public static Result<T> Failure(
        string error,
        HttpStatusCode code = HttpStatusCode.BadRequest
    ) => new(false, default, error);
}

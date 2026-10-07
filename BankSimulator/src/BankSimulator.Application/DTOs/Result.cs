using System.Net;

namespace BankSimulator.Application;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    public HttpStatusCode StatusCode { get; }

    protected Result(bool success, T value, string error, HttpStatusCode code)
    {
        IsSuccess = success;
        Value = value;
        Error = error;
        StatusCode = code;
    }

    public static Result<T> Success(T value, HttpStatusCode code = HttpStatusCode.OK) =>
        new(true, value, default, code);

    public static Result<T> Failure(
        string error,
        HttpStatusCode code = HttpStatusCode.BadRequest
    ) => new(false, default, error, code);
}

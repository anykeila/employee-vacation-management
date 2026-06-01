namespace VacationManagement.Application.Common;

public enum ResultError
{
    None,
    NotFound,
    Conflict,
    Validation,
    Forbidden
}

public class Result<T>
{
    private Result(T? value, ResultError error, string? message)
    {
        Value = value;
        Error = error;
        Message = message;
    }

    public T? Value { get; }
    public ResultError Error { get; }
    public string? Message { get; }
    public bool Succeeded => Error == ResultError.None;

    public static Result<T> Success(T value) => new(value, ResultError.None, null);
    public static Result<T> Failure(ResultError error, string message) => new(default, error, message);
}

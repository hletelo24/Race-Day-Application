namespace front_end_web.Models.Auth;

public class AuthServiceResult<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public T? Data { get; init; }

    public static AuthServiceResult<T> Ok(T data, string message = "")
        => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

    public static AuthServiceResult<T> Fail(string message)
        => new()
        {
            Success = false,
            Message = message
        };
}
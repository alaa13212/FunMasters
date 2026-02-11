namespace FunMasters.Shared.DTOs;

public class ApiResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static ApiResult Ok() => new() { Success = true };
    public static ApiResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

public class ApiResult<T> : ApiResult
{
    public T? Data { get; set; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public new static ApiResult<T> Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

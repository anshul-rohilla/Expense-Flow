using System.Collections.Generic;
using System.Linq;

namespace Expense_Flow.Models;

public class ServiceResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? Message { get; set; }

    public static ServiceResult<T> SuccessResult(T data, string? message = null)
    {
        return new ServiceResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ServiceResult<T> FailureResult(string error)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }

    public static ServiceResult<T> FailureResult(List<string> errors)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Errors = errors
        };
    }

    public string GetErrorMessage()
    {
        return Errors.Any() ? string.Join(", ", Errors) : "An error occurred.";
    }
}

namespace Floom.Base;

public class FloomOperationResult<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }

    // Factory methods for a successful result
    public static FloomOperationResult<T> CreateSuccessResult(T data)
    {
        return new FloomOperationResult<T> { Success = true, Data = data };
    }

    public static FloomOperationResult<T> CreateSuccessResult()
    {
        return new FloomOperationResult<T> { Success = true };
    }

    // Factory method for a failed result
    public static FloomOperationResult<T> CreateFailure(string errorMessage)
    {
        return new FloomOperationResult<T> { Success = false, Message = errorMessage };
    }

}
namespace MTM_Waitlist_Server.Api.Models;

/// <summary>
/// Represents the success or failure of a server-side data operation.
/// </summary>
public sealed class Model_Dao_Result
{
    private Model_Dao_Result(bool isSuccess, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Returns the human-readable failure reason when <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Model_Dao_Result Success() => new(true, string.Empty);

    /// <summary>
    /// Creates a failed result with a user-safe error message.
    /// </summary>
    public static Model_Dao_Result Failure(string errorMessage) => new(false, errorMessage);
}

/// <summary>
/// Represents the success or failure of a server-side data operation that returns a value.
/// </summary>
public sealed class Model_Dao_Result<T>
{
    private Model_Dao_Result(bool isSuccess, T? data, string errorMessage)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Indicates whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Returns the payload when the operation succeeds.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Returns the human-readable failure reason when <see cref="IsSuccess"/> is <see langword="false"/>.
    /// </summary>
    public string ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result with the supplied payload.
    /// </summary>
    public static Model_Dao_Result<T> Success(T? data) => new(true, data, string.Empty);

    /// <summary>
    /// Creates a failed result with a user-safe error message.
    /// </summary>
    public static Model_Dao_Result<T> Failure(string errorMessage) => new(false, default, errorMessage);
}
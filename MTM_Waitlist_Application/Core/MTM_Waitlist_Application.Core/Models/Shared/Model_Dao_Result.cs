namespace MTM_Waitlist_Application.Core.Models.Shared;

/// <summary>
/// Standard result envelope for operations that return data (get, list).
/// All repository and service methods return this type — never throw exceptions.
/// </summary>
/// <typeparam name="T">The type of data returned on success.</typeparam>
public sealed class Model_Dao_Result<T>
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>The returned data when the operation succeeded; <see langword="null"/> on failure.</summary>
    public T? Data { get; init; }

    /// <summary>Human-readable error description when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Creates a successful result carrying <paramref name="data"/>.</summary>
    public static Model_Dao_Result<T> Success(T data) =>
        new() { IsSuccess = true, Data = data };

    /// <summary>Creates a failure result with a descriptive <paramref name="message"/>.</summary>
    public static Model_Dao_Result<T> Failure(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };
}

/// <summary>
/// Standard result envelope for void operations (insert, update, delete).
/// All repository and service methods return this type — never throw exceptions.
/// </summary>
public sealed class Model_Dao_Result
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Human-readable error description when <see cref="IsSuccess"/> is <see langword="false"/>.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Creates a successful result.</summary>
    public static Model_Dao_Result Success() =>
        new() { IsSuccess = true };

    /// <summary>Creates a failure result with a descriptive <paramref name="message"/>.</summary>
    public static Model_Dao_Result Failure(string message) =>
        new() { IsSuccess = false, ErrorMessage = message };
}

using Core.Models.Shared;

namespace Core.Interfaces.Api;

/// <summary>
/// Contract for all HTTP communication with the backend REST API.
/// Implementations must catch all network and HTTP exceptions internally
/// and return <see cref="Model_Dao_Result"/> — never propagate exceptions to callers.
/// </summary>
public interface IApiClient
{
    /// <summary>Sends an HTTP GET request to <paramref name="endpoint"/> and deserialises the response as <typeparamref name="T"/>.</summary>
    Task<Model_Dao_Result<T>> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>Sends an HTTP POST request carrying <paramref name="payload"/> to <paramref name="endpoint"/> and deserialises the response as <typeparamref name="T"/>.</summary>
    Task<Model_Dao_Result<T>> PostAsync<T>(string endpoint, object payload, CancellationToken cancellationToken = default);

    /// <summary>Sends an HTTP PUT request carrying <paramref name="payload"/> to <paramref name="endpoint"/> and deserialises the response as <typeparamref name="T"/>.</summary>
    Task<Model_Dao_Result<T>> PutAsync<T>(string endpoint, object payload, CancellationToken cancellationToken = default);

    /// <summary>Sends an HTTP DELETE request to <paramref name="endpoint"/>.</summary>
    Task<Model_Dao_Result> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}

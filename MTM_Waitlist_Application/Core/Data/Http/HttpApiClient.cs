using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Core.Constants.Api;
using Core.Interfaces.Api;
using Core.Models.Shared;

namespace Data.Http;

/// <summary>
/// HTTP implementation of <see cref="IApiClient"/> that communicates with the
/// backend REST API on the internal work network (plain HTTP — the API is not
/// exposed to the public internet).
///
/// <para>
/// <b>URL selection:</b> every request first targets the primary URL
/// (<c>http://172.16.1.104:5000</c>).  If the primary host is unreachable
/// (connection refused, no route to host, or probe timeout) the client
/// automatically retries against the fallback URL (<c>http://localhost:5000</c>).
/// This lets developers run the API locally when they are not on the work LAN.
/// </para>
///
/// <para>
/// <b>Android emulator:</b> "localhost" inside the emulator points to the
/// emulator itself, not the host PC.  <c>HttpApiClient</c> transparently
/// rewrites <c>localhost</c> to <c>10.0.2.2</c> on the Android target.
/// </para>
///
/// Every outbound request attaches the JWT stored in <c>SecureStorage</c>
/// as a Bearer token.  All network and serialisation exceptions are caught and
/// returned as <see cref="Model_Dao_Result"/> failures — never propagated.
/// </summary>
public sealed class HttpApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _primaryUrl;
    private readonly string _fallbackUrl;

    /// <summary>
    /// How long to wait for a connection to the primary host before switching
    /// to the fallback.  Kept short so the app does not feel frozen while on
    /// a dev machine with no work-LAN route.
    /// </summary>
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Initialises a new instance using URL values from <paramref name="configuration"/>.
    /// Falls back to compile-time constants if the configuration keys are absent.
    /// </summary>
    public HttpApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _primaryUrl = configuration["Api:PrimaryBaseUrl"] ?? Constants_Api.PrimaryBaseUrl;
        _fallbackUrl = configuration["Api:FallbackBaseUrl"] ?? Constants_Api.FallbackBaseUrl;

        // Android emulator uses 10.0.2.2 to reach the host PC's localhost.
#if ANDROID
        _fallbackUrl = _fallbackUrl.Replace("localhost", "10.0.2.2", StringComparison.OrdinalIgnoreCase);
#endif
    }

    // ── Public IApiClient methods ──────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<T>> GetAsync<T>(
        string endpoint, CancellationToken cancellationToken = default)
    {
        var (url, client) = await ResolveBaseUrlAsync(cancellationToken);
        try
        {
            using var request = await BuildRequestAsync(HttpMethod.Get, url, endpoint);
            using var response = await client.SendAsync(request, cancellationToken);
            return await ReadResultAsync<T>(response, cancellationToken);
        }
        catch (Exception ex) { return Fail<T>(ex); }
        finally { client.Dispose(); }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<T>> PostAsync<T>(
        string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        var (url, client) = await ResolveBaseUrlAsync(cancellationToken);
        try
        {
            using var request = await BuildRequestAsync(HttpMethod.Post, url, endpoint);
            request.Content = JsonContent.Create(payload);
            using var response = await client.SendAsync(request, cancellationToken);
            return await ReadResultAsync<T>(response, cancellationToken);
        }
        catch (Exception ex) { return Fail<T>(ex); }
        finally { client.Dispose(); }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result<T>> PutAsync<T>(
        string endpoint, object payload, CancellationToken cancellationToken = default)
    {
        var (url, client) = await ResolveBaseUrlAsync(cancellationToken);
        try
        {
            using var request = await BuildRequestAsync(HttpMethod.Put, url, endpoint);
            request.Content = JsonContent.Create(payload);
            using var response = await client.SendAsync(request, cancellationToken);
            return await ReadResultAsync<T>(response, cancellationToken);
        }
        catch (Exception ex) { return Fail<T>(ex); }
        finally { client.Dispose(); }
    }

    /// <inheritdoc/>
    public async Task<Model_Dao_Result> DeleteAsync(
        string endpoint, CancellationToken cancellationToken = default)
    {
        var (url, client) = await ResolveBaseUrlAsync(cancellationToken);
        try
        {
            using var request = await BuildRequestAsync(HttpMethod.Delete, url, endpoint);
            using var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return Model_Dao_Result.Success();
        }
        catch (HttpRequestException ex) { return Model_Dao_Result.Failure($"Network error: {ex.Message}"); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Model_Dao_Result.Failure("The request timed out."); }
        catch (Exception ex) { return Model_Dao_Result.Failure($"Unexpected error: {ex.Message}"); }
        finally { client.Dispose(); }
    }

    // ── Private helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Probes the primary URL with a short timeout.  Returns the primary URL
    /// (and a fresh <see cref="HttpClient"/>) if reachable; otherwise returns
    /// the fallback URL so the caller does not have to know which host was chosen.
    /// </summary>
    /// <remarks>
    /// The probe is a lightweight HEAD request to the API health endpoint.
    /// If the primary host is simply offline (connection refused) the OS returns
    /// immediately; the <see cref="ProbeTimeout"/> only guards against a route
    /// that exists but is very slow.
    /// </remarks>
    private async Task<(string baseUrl, HttpClient client)> ResolveBaseUrlAsync(
        CancellationToken cancellationToken)
    {
        using var probeClient = _httpClientFactory.CreateClient();
        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeCts.CancelAfter(ProbeTimeout);

        try
        {
            // HEAD to /health avoids triggering any side-effects on the server.
            using var probe = new HttpRequestMessage(HttpMethod.Head, $"{_primaryUrl}/health");
            using var resp = await probeClient.SendAsync(probe, HttpCompletionOption.ResponseHeadersRead, probeCts.Token);
            // Any HTTP response (even 404/405) means the host is reachable.
            return (_primaryUrl, _httpClientFactory.CreateClient());
        }
        catch
        {
            // Primary unreachable — silently switch to fallback.
            return (_fallbackUrl, _httpClientFactory.CreateClient());
        }
    }

    /// <summary>
    /// Creates an <see cref="HttpRequestMessage"/>, attaching the JWT from
    /// <c>SecureStorage</c> as a Bearer header when present.
    /// </summary>
    private static async Task<HttpRequestMessage> BuildRequestAsync(
        HttpMethod method, string baseUrl, string endpoint)
    {
        var request = new HttpRequestMessage(method, $"{baseUrl}{endpoint}");

        var token = await SecureStorage.GetAsync("auth_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    /// <summary>
    /// Reads the HTTP response body and deserialises it as <typeparamref name="T"/>.
    /// Returns a typed <see cref="Model_Dao_Result{T}"/> — never throws.
    /// </summary>
    private static async Task<Model_Dao_Result<T>> ReadResultAsync<T>(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return data is not null
                ? Model_Dao_Result<T>.Success(data)
                : Model_Dao_Result<T>.Failure("The server returned no data.");
        }
        catch (HttpRequestException ex)
        { return Model_Dao_Result<T>.Failure($"Network error: {ex.Message}"); }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Model_Dao_Result<T>.Failure("The request timed out."); }
        catch (Exception ex)
        { return Model_Dao_Result<T>.Failure($"Unexpected error: {ex.Message}"); }
    }

    private static Model_Dao_Result<T> Fail<T>(Exception ex) =>
        ex is TaskCanceledException
            ? Model_Dao_Result<T>.Failure("The request timed out.")
            : Model_Dao_Result<T>.Failure($"Unexpected error: {ex.Message}");
}

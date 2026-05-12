namespace Core.Constants.Api;

/// <summary>
/// Compile-time constants for the backend REST API.
/// The REST API runs on the same server as the MySQL database (172.16.1.104:5000)
/// on the internal work network — it is never exposed to the public internet.
/// Values here are last-resort fallbacks; the real values come from appsettings.json.
/// </summary>
public static class Constants_Api
{
    /// <summary>
    /// Primary API base URL — the internal work-network server.
    /// The REST API and MySQL database both run on this host.
    /// </summary>
    public const string PrimaryBaseUrl = "http://172.16.1.104:5000";

    /// <summary>
    /// Fallback API base URL used when the primary host is unreachable.
    /// Typically the developer's local machine during development.
    /// On Android emulator, "localhost" resolves to the emulator itself;
    /// use 10.0.2.2 to reach the host PC — handled automatically by <c>HttpApiClient</c>.
    /// </summary>
    public const string FallbackBaseUrl = "http://localhost:5000";

    /// <summary>REST endpoint for all waitlist entry operations.</summary>
    public const string WaitlistEntryEndpoint = "/api/waitlist";
}

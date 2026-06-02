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

    /// <summary>REST endpoint for client heartbeat POST.</summary>
    public const string HeartbeatEndpoint = "/api/admin/heartbeat";

    /// <summary>REST endpoint for client disconnect notification.</summary>
    public const string DisconnectEndpoint = "/api/admin/disconnect";

    /// <summary>REST endpoint for polling the shutdown signal for the calling client.</summary>
    public const string ShutdownSignalEndpoint = "/api/admin/shutdown-signal";

    /// <summary>REST endpoint for acknowledging a received shutdown signal.</summary>
    public const string ShutdownSignalAcknowledgeEndpoint = "/api/admin/shutdown-signal/acknowledge";

    /// <summary>REST endpoint for retrieving all available Infor Visual workcenters.</summary>
    public const string VisualWorkcenters = "/api/infor-visual/workcenters";

    /// <summary>REST endpoint template for retrieving a single Infor Visual work order header.</summary>
    public const string VisualWorkOrderHeader = "/api/infor-visual/work-orders/{0}";

    /// <summary>REST endpoint template for retrieving all sequences for an Infor Visual work order.</summary>
    public const string VisualWorkOrderSequences = "/api/infor-visual/work-orders/{0}/sequences";

    /// <summary>REST endpoint template for retrieving subordinate parts for a work order sequence.</summary>
    public const string VisualSubordinateParts = "/api/infor-visual/work-orders/{0}/sequences/{1}/subordinate-parts";

    /// <summary>REST endpoint template for retrieving active work orders for a workcenter.</summary>
    public const string VisualActiveWorkOrders = "/api/infor-visual/resources/{0}/active-work-orders";

    /// <summary>REST endpoint template for retrieving the current active job for a workstation.</summary>
    public const string SetupTechActiveJob = "/api/setup-tech/active-job/{0}";

    /// <summary>REST endpoint for saving the active workstation job configuration.</summary>
    public const string SetupTechSetActiveJob = "/api/setup-tech/active-job";

    /// <summary>REST endpoint template for retrieving cached dunnage assignments for a work order sequence.</summary>
    public const string SetupTechDunnageAssignment = "/api/setup-tech/dunnage-assignment/{0}/{1}";

    /// <summary>REST endpoint for inserting or updating a cached dunnage assignment line.</summary>
    public const string SetupTechUpsertDunnage = "/api/setup-tech/dunnage-assignment";

    /// <summary>REST endpoint template for deleting a cached dunnage assignment line.</summary>
    public const string SetupTechDeleteDunnage = "/api/setup-tech/dunnage-assignment/{0}";

    /// <summary>REST endpoint for retrieving enabled Setup Tech dunnage types.</summary>
    public const string SetupTechDunnageTypes = "/api/setup-tech/dunnage-types";

    /// <summary>REST endpoint template for retrieving job history for a workstation.</summary>
    public const string SetupTechJobHistory = "/api/setup-tech/job-history/{0}";

    /// <summary>REST endpoint for retrieving the cross-database dunnage catalog.</summary>
    public const string DunnageCatalog = "/api/dunnage-catalog";

    /// <summary>REST endpoint template for retrieving cached subordinate parts for a work order sequence.</summary>
    public const string SetupTechSubordinateParts = "/api/setup-tech/subordinate-parts/{0}/{1}";

}

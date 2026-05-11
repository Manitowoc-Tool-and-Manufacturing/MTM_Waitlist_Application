namespace MTM_Waitlist_Server.Core.Models.Dashboard;

/// <summary>
/// Represents all active MySQL connections for a single user,
/// grouped for display in the dashboard connections panel.
/// </summary>
public sealed class Model_ConnectionGroup
{
    /// <summary>The MySQL user name shared by all threads in this group.</summary>
    public string User { get; }

    /// <summary>The individual thread rows belonging to this user.</summary>
    public IReadOnlyList<Model_ActiveConnection> Threads { get; }

    /// <summary>Total number of open threads for this user.</summary>
    public int ThreadCount => Threads.Count;

    /// <summary>Initialises the group from a pre-built thread list.</summary>
    public Model_ConnectionGroup(string user, IReadOnlyList<Model_ActiveConnection> threads)
    {
        User = user;
        Threads = threads;
    }

    /// <summary>Builds grouped connection models from a flat raw connection list.</summary>
    public static IReadOnlyList<Model_ConnectionGroup> FromConnections(
        IEnumerable<Model_ActiveConnection> connections)
    {
        return connections
            .GroupBy(c => c.User)
            .OrderBy(g => g.Key)
            .Select(g => new Model_ConnectionGroup(g.Key, g.ToList()))
            .ToList();
    }
}

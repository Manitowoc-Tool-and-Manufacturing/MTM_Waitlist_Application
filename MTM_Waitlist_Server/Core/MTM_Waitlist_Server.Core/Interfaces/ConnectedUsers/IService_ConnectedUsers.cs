using MTM_Waitlist_Server.Core.Models.ConnectedUsers;

namespace MTM_Waitlist_Server.Core.Interfaces.ConnectedUsers;

/// <summary>
/// Provides access to connected-user data sourced from the MySQL database.
/// Returns all users and their associated workstation information so the admin
/// can see who is currently registered in the system.
/// </summary>
public interface IService_ConnectedUsers
{
    /// <summary>
    /// Returns all active users joined with their workstation information.
    /// Users without a dedicated workstation record (personal-workstation users)
    /// will have <see cref="Model_ConnectedUser.WorkstationName"/> set to null.
    /// </summary>
    Task<IReadOnlyList<Model_ConnectedUser>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the full connection detail for a single user by ID.
    /// Returns null if the user does not exist or is inactive.
    /// </summary>
    Task<Model_ConnectedUser?> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
}

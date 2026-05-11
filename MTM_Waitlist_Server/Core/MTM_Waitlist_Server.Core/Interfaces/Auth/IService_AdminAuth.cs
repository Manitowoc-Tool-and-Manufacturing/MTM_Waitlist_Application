using System.Threading.Tasks;

namespace MTM_Waitlist_Server.Core.Interfaces.Auth;

/// <summary>
/// Checks whether the current Windows user is authorised to open the admin application
/// by querying their Role in the <c>mtm_waitlist.Users</c> table.
/// Roles <c>Admin</c> and <c>Developer</c> are permitted.
/// </summary>
public interface IService_AdminAuth
{
    /// <summary>
    /// Returns <c>true</c> if the supplied Windows username exists in the Users table
    /// with an active account and a Role of <c>Admin</c> or <c>Developer</c>.
    /// </summary>
    /// <param name="windowsUsername">
    /// The Windows identity name, e.g. <c>DOMAIN\jsmith</c> or <c>jsmith</c>.
    /// </param>
    Task<bool> IsAuthorisedAsync(string windowsUsername);
}

using Core.Models.SetupTech;
using Core.Models.Shared;

namespace Core.Interfaces.SetupTech;

/// <summary>
/// Reads available dunnage catalog items used by the Setup Tech picker UI.
/// </summary>
public interface IRepository_DunnageCatalog
{
    /// <summary>Returns all available dunnage catalog items.</summary>
    Task<Model_Dao_Result<List<Model_DunnagePart>>> GetDunnageCatalogAsync(CancellationToken cancellationToken = default);
}
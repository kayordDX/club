using Club.Common.Models;

namespace Club.Features.Outlet.GetAll;

public class OutletGetAllRequest : QueryModel
{
    /// <summary>
    /// Optional free-text search term. When provided, results are filtered using
    /// PostgreSQL full-text search over the outlet's name, display name, description,
    /// address and tags.
    /// </summary>
    public string? Search { get; set; }
}

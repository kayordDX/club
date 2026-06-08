using Microsoft.Extensions.Primitives;

namespace Club.Common.Auth;

public interface ICurrentFacilityAccessor
{
    bool TryGetFacilityId(out int facilityId);
}

public class CurrentFacilityAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentFacilityAccessor
{
    private const string CacheKey = "__current_facility_id";
    private static readonly string[] RouteKeys = ["facilityId", "FacilityId"];
    private static readonly string[] HeaderKeys = ["X-Facility-Id", "facilityId", "FacilityId"];

    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public bool TryGetFacilityId(out int facilityId)
    {
        facilityId = default;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return false;
        }

        if (httpContext.Items.TryGetValue(CacheKey, out var cachedFacilityId) && cachedFacilityId is int cached)
        {
            facilityId = cached;
            return true;
        }

        var routeFacilityId = TryGetRouteFacilityId(httpContext);
        var headerFacilityId = TryGetHeaderFacilityId(httpContext);

        if (routeFacilityId.HasValue && headerFacilityId.HasValue && routeFacilityId != headerFacilityId)
        {
            return false;
        }

        var resolvedFacilityId = routeFacilityId ?? headerFacilityId;
        if (!resolvedFacilityId.HasValue)
        {
            return false;
        }

        facilityId = resolvedFacilityId.Value;
        httpContext.Items[CacheKey] = facilityId;
        return true;
    }

    private static int? TryGetRouteFacilityId(HttpContext httpContext)
    {
        foreach (var key in RouteKeys)
        {
            if (httpContext.Request.RouteValues.TryGetValue(key, out var value) && value != null &&
                int.TryParse(value.ToString(), out var facilityId))
            {
                return facilityId;
            }
        }

        return null;
    }

    private static int? TryGetHeaderFacilityId(HttpContext httpContext)
    {
        foreach (var key in HeaderKeys)
        {
            if (httpContext.Request.Headers.TryGetValue(key, out StringValues value) &&
                int.TryParse(value.ToString(), out var facilityId))
            {
                return facilityId;
            }
        }

        return null;
    }
}

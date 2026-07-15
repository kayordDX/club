using Microsoft.EntityFrameworkCore;
using Club.Data;
using Club.Common.Payments;

namespace Club.Features.Facility.PaymentMethods;

public class Endpoint(AppDbContext dbContext) : Endpoint<FacilityPaymentMethodsRequest, List<FacilityPaymentMethodsResponse>>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Get("/facility/{facilityId}/payment-methods");
        Description(x => x.WithName("FacilityPaymentMethods"));
        AllowAnonymous();
    }

    public override async Task HandleAsync(FacilityPaymentMethodsRequest req, CancellationToken ct)
    {
        var results = await _dbContext.PaymentProviderConfig
            .AsNoTracking()
            .Where(c => c.FacilityId == req.FacilityId && c.Enabled == true)
            .Select(c => new FacilityPaymentMethodsResponse
            {
                ProviderName = PaymentOptionsRegistry.GetProviderName(c.Type),
                Type = c.Type.ToString(),
            })
            .ToListAsync(ct);

        await Send.OkAsync(results, ct);
    }
}

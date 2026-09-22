using Club.Data;
using Club.Entities;
using Club.Features.Admin.Contract;
using Microsoft.EntityFrameworkCore;

namespace Club.Features.Admin.Contract.Create;

public class Endpoint(AppDbContext dbContext) : Endpoint<AdminContractCreateRequest, AdminContractDTO>
{
    private readonly AppDbContext _dbContext = dbContext;

    public override void Configure()
    {
        Post("/admin/facility/{FacilityId}/contract");
        Description(x => x.WithName("AdminContractCreate"));
        Policies(Constants.Policy.Manager);
    }

    public override async Task HandleAsync(AdminContractCreateRequest req, CancellationToken ct)
    {
        var facility = await _dbContext.Facility.FirstOrDefaultAsync(f => f.Id == req.FacilityId, ct);

        if (facility is null)
        {
            AddError(r => r.FacilityId, "Facility not found.");
            await Send.ErrorsAsync(404, ct);
            return;
        }

        var contract = new Entities.Contract
        {
            Name = req.Name,
            Price = req.Price,
            Frequency = req.Frequency,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            IsActive = req.IsActive,
            IsPublic = req.IsPublic,
        };

        contract.ContractFacilities.Add(new ContractFacility { Contract = contract, Facility = facility });

        _dbContext.Contract.Add(contract);
        await _dbContext.SaveChangesAsync(ct);

        var dto = new AdminContractDTO
        {
            Id = contract.Id,
            Name = contract.Name,
            Price = contract.Price,
            Frequency = contract.Frequency,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            IsActive = contract.IsActive,
            IsPublic = contract.IsPublic,
        };

        await Send.OkAsync(dto, ct);
    }
}

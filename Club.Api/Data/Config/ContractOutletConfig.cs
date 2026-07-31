using Club.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Club.Data.Config;

public class ContractOutletConfig : IEntityTypeConfiguration<ContractOutlet>
{
    public void Configure(EntityTypeBuilder<ContractOutlet> builder) { }
}

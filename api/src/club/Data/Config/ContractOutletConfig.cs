using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Online.Entities;

namespace Online.Data.Config;

public class ContractOutletConfig : IEntityTypeConfiguration<ContractOutlet>
{
    public void Configure(EntityTypeBuilder<ContractOutlet> builder)
    {

    }
}

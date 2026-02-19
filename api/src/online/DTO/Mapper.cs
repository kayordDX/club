using Riok.Mapperly.Abstractions;

namespace Online.DTO;

[Mapper]
public static partial class Mapper
{
    public static partial IQueryable<OutletDTO> ProjectToDto(this IQueryable<Entities.Outlet> q);
}

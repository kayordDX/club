using Riok.Mapperly.Abstractions;

namespace Club.DTO;

[Mapper]
public static partial class Mapper
{
    public static partial IQueryable<OutletDTO> ProjectToDto(this IQueryable<Entities.Outlet> q);
    public static partial IQueryable<BookingDTO> ProjectToDto(this IQueryable<Entities.Booking> q);
    public static partial IQueryable<FacilityDTO> ProjectToDto(this IQueryable<Entities.Facility> q);
}

using Microsoft.EntityFrameworkCore;
using Online.Common;
using Online.Entities;

namespace Online.Data;

public static class SeedDbContext
{
    public static async Task SeedData(AppDbContext dbContext, CancellationToken ct)
    {
        // Temp drop all for now
        // await db.Database.ExecuteSqlAsync($"""
        //     DROP SCHEMA public CASCADE;
        //     CREATE SCHEMA public;
        //     GRANT ALL ON SCHEMA public TO online;
        // """, ct);

        await dbContext.Database.ExecuteSqlRawAsync("delete from slot_contract_booking;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from extra_booking;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from slot;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from resource;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from contract;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from contract_field;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from validation;", ct);
        await dbContext.Business.ExecuteDeleteAsync(ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE validation_id_seq RESTART WITH 1;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE contract_id_seq RESTART WITH 1;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE facility_id_seq RESTART WITH 1;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("delete from facility_type;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE resource_id_seq RESTART WITH 1;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE facility_type_id_seq RESTART WITH 1;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE outlet_id_seq RESTART WITH 1;", ct);

        await dbContext.Database.ExecuteSqlRawAsync("delete from booking_status;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE booking_status_id_seq RESTART WITH 4;", ct);

        await dbContext.BookingStatus.AddRangeAsync(
        [
            new BookingStatus { Id = BookingStatuses.PendingId, Name = BookingStatuses.PendingName },
            new BookingStatus { Id = BookingStatuses.ConfirmedId, Name = BookingStatuses.ConfirmedName },
            new BookingStatus { Id = BookingStatuses.CancelledId, Name = BookingStatuses.CancelledName }
        ], ct);

        var paymentTypes = await dbContext.PaymentType
            .Select(x => x.Name)
            .ToListAsync(ct);

        if (!paymentTypes.Contains("Pay on arrival"))
        {
            await dbContext.PaymentType.AddAsync(new PaymentType { Name = "Pay on arrival" }, ct);
        }

        if (!paymentTypes.Contains("Credit card"))
        {
            await dbContext.PaymentType.AddAsync(new PaymentType { Name = "Credit card" }, ct);
        }

        await dbContext.SaveChangesAsync(ct);


        if (!dbContext.Business.Any())
        {
            var business = new Business { Name = "Business" };
            var outletType = new OutletType { Name = "Golf Course" };
            var outlet = new Outlet { Name = "Ruimsig Country Club", Slug = "ruimsig-country-club", DisplayName = "RCC", Business = business, OutletType = outletType, VatNumber = "1234567", IsActive = true };
            await dbContext.Outlet.AddAsync(outlet, ct);
            var facilityTypeGolf = new FacilityType { Name = "Golf Course" };
            var facilityTypePaddle = new FacilityType { Name = "Paddle" };
            var facility1 = new Facility { Name = "Ruimsig Golf Course", IsActive = true, OutletId = 1, FacilityType = facilityTypeGolf, Outlet = outlet };
            var facility2 = new Facility { Name = "Ruimsig Paddle Court", IsActive = true, OutletId = 1, FacilityType = facilityTypePaddle, Outlet = outlet };

            var resource1 = new Resource { Name = "1st", Facility = facility1 };
            var resource2 = new Resource { Name = "10th", Facility = facility1 };
            var resource3 = new Resource { Name = "Court 1", Facility = facility2 };
            var resource4 = new Resource { Name = "Court 2", Facility = facility2 };

            await dbContext.Facility.AddAsync(facility1, ct);
            await dbContext.Facility.AddAsync(facility2, ct);

            await dbContext.Resource.AddAsync(resource1, ct);
            await dbContext.Resource.AddAsync(resource2, ct);
            await dbContext.Resource.AddAsync(resource3, ct);
            await dbContext.Resource.AddAsync(resource4, ct);

            var contract1 = new Contract { Name = "Guest", Business = business };
            var contract2 = new Contract { Name = "Member", Business = business };

            await dbContext.Contract.AddAsync(contract1, ct);
            await dbContext.Contract.AddAsync(contract2, ct);

            var validation1 = new Validation { Name = "Login", Id = 1 };
            var validation2 = new Validation { Name = "HNA Verify", Id = 2 };
            await dbContext.Validation.AddAsync(validation1, ct);
            await dbContext.Validation.AddAsync(validation2, ct);

            var contractField1 = new ContractField { Id = 1, Name = "Price", FieldValidation = "decimal", Business = business };
            await dbContext.ContractField.AddAsync(contractField1, ct);

            // Create hourly slots for the entire day for all resources
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var resources = new[] { resource1, resource2, resource3, resource4 };

            foreach (var resource in resources)
            {
                for (int hour = 8; hour < 11; hour++)
                {
                    var startTime = today.AddHours(hour);
                    var endTime = startTime.AddHours(1);

                    if (resource.Name == "1st" || resource.Name == "10th")
                    {
                        var id = Guid.CreateVersion7();
                        await dbContext.Slot.AddAsync(new Slot { Id = id, StartDatetime = startTime, EndDatetime = endTime, Resource = resource, Facility = resource.Facility, MaxBookings = 4 }, ct);
                        await dbContext.SlotContract.AddAsync(new SlotContract { Contract = contract1, Price = 50, SlotId = id, Validation = validation2, Description = "9 Holes" }, ct);
                        await dbContext.SlotContract.AddAsync(new SlotContract { Contract = contract1, Price = 100, SlotId = id, Validation = validation2, Description = "18 Holes" }, ct);
                    }
                    else
                    {
                        var id = Guid.CreateVersion7();
                        await dbContext.Slot.AddAsync(new Slot { Id = id, StartDatetime = startTime, EndDatetime = endTime, Resource = resource, Facility = resource.Facility }, ct);
                        await dbContext.SlotContract.AddAsync(new SlotContract { Contract = contract2, Price = 40, SlotId = id, Validation = validation1, Description = "Member" }, ct);
                        await dbContext.SlotContract.AddAsync(new SlotContract { Contract = contract2, Price = 100, SlotId = id, Validation = validation1, Description = "Guest" }, ct);
                    }
                }
            }
            await dbContext.SaveChangesAsync(ct);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Club.Common;
using Club.Common.Enums;
using Club.Entities;

namespace Club.Data;

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
        await dbContext.Database.ExecuteSqlRawAsync("delete from extra;", ct);
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
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE extra_id_seq RESTART WITH 1;", ct);

        await dbContext.Database.ExecuteSqlRawAsync("delete from booking_status;", ct);
        await dbContext.Database.ExecuteSqlRawAsync("ALTER SEQUENCE booking_status_id_seq RESTART WITH 4;", ct);

        await dbContext.BookingStatus.AddRangeAsync(
        [
            new BookingStatus { Id = (int)BookingStatusEnum.Pending, Name = BookingStatusEnum.Pending.ToString() },
            new BookingStatus { Id = (int)BookingStatusEnum.Confirmed, Name = BookingStatusEnum.Confirmed.ToString() },
            new BookingStatus { Id = (int)BookingStatusEnum.Cancelled, Name = BookingStatusEnum.Cancelled.ToString() },
            new BookingStatus { Id = (int)BookingStatusEnum.Expired, Name = BookingStatusEnum.Expired.ToString() }
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
            var outlet = new Outlet
            {
                Name = "Ruimsig Country Club",
                Slug = "ruimsig-country-club",
                DisplayName = "RCC",
                Business = business,
                OutletType = outletType,
                VatNumber = "1234567",
                IsActive = true,
                Description = """
                An exclusive 18hole course with many more facilities

                ### Features
                - Fastest service
                - Nicest golf course
                """,
                Address = "Hole-In-One Ave, Ruimsig, Muldersdrift, 1732",
                Contact = "011 958 1905",
                Email = "cc@ruimsigcc.co.za",
                Tags = "18hole,Paddle,Restaurant",
                OperatingHours = """
                | Day | Hours |
                | --- | --- |
                | Monday | Closed |
                | Tuesday | 6:30 am–8 pm |
                | Wednesday | 6:30 am–8 pm |
                | Thursday | 6:30 am–8 pm |
                | Friday | 6:30 am–8 pm |
                | Saturday | 6:30 am–8 pm |
                | Sunday | 6:30 am–8 pm |
                """
            };
            await dbContext.Outlet.AddAsync(outlet, ct);
            var facilityTypeGolf = new FacilityType { Name = "Golf Course" };
            var facilityTypePaddle = new FacilityType { Name = "Paddle" };
            var facility1 = new Facility
            {
                Name = "Ruimsig Golf Course",
                IsActive = true,
                FacilityType = facilityTypeGolf,
                Outlet = outlet,
                Contact = "011 958 1905",
                Email = "golf@ruimsig.co.za",
                Rules = """
                # The Essential Rules of Golf: A Quick Reference Guide

                ## 1. Core Principles

                * **Play the course as you find it:** Don't alter the terrain, bend branches, or break growing things to improve your lie.
                * **Play the ball as it lies:** Don't move, touch, or improve the position of your ball unless a specific rule allows it.
                * **Act with integrity:** Golf relies on the player to be honest, call penalties on themselves, and play by the spirit of the game.

                ---

                ## 2. Teeing Area

                * **Stay within the boundaries:** You must tee off between the two tee markers and no more than **two club-lengths** behind them. You *cannot* tee off in front of the markers.
                * **Accidental knocks:** If your ball falls off the tee before you’ve made a stroke (a swing intended to hit the ball), you can re-tee it without penalty.

                ---

                ## 3. General Area & Bunker Rules

                ### Fairway and Rough

                * **Identifying your ball:** You may lift your ball to identify it, but you must mark its spot first and not clean it any more than necessary to see your mark.
                * **Loose impediments:** You can remove natural objects (leaves, stones, twigs) anywhere on the course, including in bunkers and penalty areas, as long as your ball doesn't move.

                ### Bunkers (Sand Traps)

                * **No touching the sand:** You *cannot* touch the sand with your hand or club directly in front of or behind the ball, during a practice swing, or during your backswing.
                * **Unplayable ball in bunker:** If you declare your ball unplayable, you can drop a ball in the bunker for a **1-stroke penalty**, or drop *outside* the bunker going back on a straight line from the pin for a **2-stroke penalty**.

                ---

                ## 4. Penalty Areas (Red and Yellow Hazards)

                If your ball lands in a water hazard or designated penalty area, you have options:

                1. **Play it as it lies:** You can play the ball out of the hazard without grounding your club penalty-free.
                2. **Relief options (1-stroke penalty):**
                * **Stroke-and-Distance:** Go back and replay the shot from where you last hit.
                * **Back-on-the-line:** Drop a ball any distance behind the penalty area, keeping the point where the ball crossed into the hazard directly between you and the hole.
                * **Lateral Relief (Red Hazards Only):** Drop within **two club-lengths** of where the ball last crossed the edge of the penalty area, no closer to the hole.

                ---

                ## 5. Putting Green

                * **Marking and cleaning:** You may lift, mark, and clean your ball once it is on the putting green.
                * **Repairing damage:** You can repair ball marks, old hole plugs, and damage from spike shoes, but you cannot repair natural wear or aeration holes.
                * **Hitting the flagstick:** There is **no penalty** if you putt from the green and your ball hits the flagstick left in the hole.

                ---

                ## 6. Out of Bounds (OB) and Lost Balls

                * **Search time:** You have a maximum of **3 minutes** to search for a ball before it is officially lost.
                * **Out of Bounds:** Marked by white stakes or lines.
                * **Standard Procedure (1-stroke penalty):** You must take "Stroke-and-Distance" relief. Go back to where you hit the previous shot and hit again.
                * *Alternative Local Rule:* If in effect, you can drop a ball in the fairway adjacent to where your ball went out of bounds or was lost, taking a **2-stroke penalty**.

                ---

                ## 7. Taking Relief (Dropping the Ball)

                When taking relief (free or penalty), follow the **Rule of Knee**:

                * **The Drop:** Hold the ball at **knee height** and let it drop straight down.
                * **Relief Area:** The ball must land in and stay within the designated relief area (usually 1 or 2 club-lengths, depending on the rule).
                * **If it rolls away:** If the dropped ball rolls out of the relief area, drop it a second time. If it happens again, place the ball exactly where it first struck the ground on the second drop.

                ---

                ## Quick Penalty Reference Table

                | Situation | Penalty | Action |
                | --- | --- | --- |
                | **Accidental ball movement by player** | 1 Stroke | Replace the ball on its original spot |
                | **Ball completely lost / Out of Bounds** | 1 Stroke + Distance | Return to previous spot to re-play |
                | **Taking relief from a Penalty Area** | 1 Stroke | Use back-on-the-line or lateral relief |
                | **Declaring a ball unplayable** | 1 Stroke | Take relief within 2 club-lengths or back-on-the-line |
                | **Playing the wrong ball** | 2 Strokes (Match play: Loss of hole) | Correct the mistake by finding and playing your ball |
                """,
                OperatingHours = """
                | Day | Hours |
                | --- | --- |
                | Monday | Closed |
                | Tuesday | 6:30 am–8 pm |
                | Wednesday | 6:30 am–8 pm |
                | Thursday | 6:30 am–8 pm |
                | Friday | 6:30 am–8 pm |
                | Saturday | 6:30 am–8 pm |
                | Sunday | 6:30 am–8 pm |
                """
            };
            var facility2 = new Facility
            {
                Name = "Ruimsig Paddle Court",
                IsActive = true,
                FacilityType = facilityTypePaddle,
                Outlet = outlet,
                Contact = "011 958 1905",
                Email = "paddle@ruimsig.co.za",
                Rules = """
                # The Essential Rules of Padel: A Quick Reference Guide

                ## 1. Court & Equipment Basics

                * **The Setup:** Padel is almost exclusively played as a **doubles game** (2 vs 2) on an enclosed court that is roughly 25% smaller than a traditional tennis court.
                * **Underhand Only:** All serves must be hit underhand.
                * **The Walls:** The glass and mesh walls enclosing the court are active parts of the game, meaning the ball can be played off them under specific conditions.

                ---

                ## 2. The Serve

                * **Positioning:** The server must stand behind the service line and stay to the left or right of the center line, serving diagonally into the opponent's opposite service box.
                * **The Execution:** The server must bounce the ball on the ground behind the service line and strike it **at or below waist height**.
                * **The Bounce:** The served ball must land directly inside the opponent's diagonal service box.
                * **The Walls on Serve:**
                * If the ball bounces in the box and hits the **glass wall**, it is a good serve.
                * If the ball bounces in the box and hits the **wire mesh fence**, it is a fault.
                * If the ball hits the net and lands in the proper box, it is a **let** and is replayed.


                * **Two Chances:** Just like tennis, the server gets a first and second serve.

                ---

                ## 3. General Play & Wall Rules

                * **The Ground First:** During a rally, the ball must always bounce on the opponent's ground *before* hitting any of their walls or fences. If it hits a wall or fence directly without bouncing first, it is out.
                * **Volleys:** Players can volley the ball (hit it before it bounces) at any point during a rally, except directly returning a serve.
                * **Using Your Own Glass:** You are allowed to hit the ball against the **glass walls on your own side** of the court to send it over the net to the opponent's side. You *cannot* hit it against your own wire mesh fence.
                * **Double Bounces:** Once the ball bounces twice on either side, the point is over.

                ---

                ## 4. Scoring System

                Padel uses the exact same scoring structure as traditional tennis:

                * **Points:** `0` $\rightarrow$ `15` $\rightarrow$ `30` $\rightarrow$ `40` $\rightarrow$ `Game`.
                * **Deuce:** At 40-40, a team must win by two consecutive points (Advantage $\rightarrow$ Game).
                * *Alternative Golden Point Rule:* Some tournaments use a "Golden Point" (sudden death) at 40-40, where the receiving team chooses which side to receive, and whoever wins that single point wins the game.
                * **Sets and Matches:** A standard match is typically the **best of 3 sets**. Each set is won by the first team to reach 6 games (leading by 2). If the score reaches 6-6, a 7-point tiebreak decides the set.
                """,
                OperatingHours = """
                | Day | Hours |
                | --- | --- |
                | Monday | Closed |
                | Tuesday | 6:30 am–8 pm |
                | Wednesday | 6:30 am–8 pm |
                | Thursday | 6:30 am–8 pm |
                | Friday | 6:30 am–8 pm |
                | Saturday | 6:30 am–8 pm |
                | Sunday | 6:30 am–8 pm |
                """
            };

            var resource1 = new Resource { Name = "1st", Facility = facility1 };
            var resource2 = new Resource { Name = "10th", Facility = facility1 };
            var resource3 = new Resource { Name = "Court 1", Facility = facility2 };
            var resource4 = new Resource { Name = "Court 2", Facility = facility2 };

            await dbContext.Facility.AddAsync(facility1, ct);
            await dbContext.Facility.AddAsync(facility2, ct);

            await dbContext.SaveChangesAsync(ct);

            await dbContext.Extra.AddAsync(new Extra { Name = "Golf Cart", Code = "GOLF_CART", FacilityId = facility1.Id, IsAvailable = true, IsOnline = true, OutletId = outlet.Id, Price = 300 }, ct);
            await dbContext.Extra.AddAsync(new Extra { Name = "Push Cart", Code = "PUSH_CART", FacilityId = facility1.Id, IsAvailable = true, IsOnline = true, OutletId = outlet.Id, Price = 100 }, ct);
            await dbContext.Extra.AddAsync(new Extra { Name = "Racket", Code = "Racket", FacilityId = facility2.Id, IsAvailable = true, IsOnline = true, OutletId = outlet.Id, Price = 70 }, ct);
            await dbContext.Extra.AddAsync(new Extra { Name = "Kids Racket", Code = "Kids", FacilityId = facility2.Id, IsAvailable = true, IsOnline = true, OutletId = outlet.Id, Price = 30 }, ct);

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

            // Create hourly slots for today and the next 7 days with slight daily variation
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var resources = new[] { resource1, resource2, resource3, resource4 };
            var random = new Random();

            for (int dayOffset = 0; dayOffset <= 7; dayOffset++)
            {
                var day = today.AddDays(dayOffset);
                var startHour = 8 + random.Next(0, 2);
                var slotCount = random.Next(3, 6);

                foreach (var resource in resources)
                {
                    var resourceStartHour = startHour;

                    if (resource.Name == "Court 2")
                    {
                        resourceStartHour += random.Next(0, 2);
                    }

                    for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
                    {
                        var startTime = day.AddHours(resourceStartHour + slotIndex);
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
            }

            await dbContext.SaveChangesAsync(ct);
        }
    }
}

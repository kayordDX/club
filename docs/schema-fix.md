# Schema Fixes & Recommendations

> Companion to [`schema.md`](./schema.md). Each item: **why it matters → recommended fix → effort**. Ordered by risk. Nothing here is implemented yet; group changes into a few reviewed migrations (never auto-apply).

## P0 — will cause incidents

### 1. Cascade delete from lookup tables
Every FK to a lookup table is `DeleteBehavior.Cascade`: `booking → booking_status`, `payment → payment_status/payment_type`, `facility → facility_type`, `outlet → outlet_type/business`, `wallet_transaction → wallet_transaction_status`, `slot_config → slot_config_type`, `extra → outlet/facility`, etc.

Deleting (or truncating) one lookup row — e.g. a stray `booking_status` delete — cascades through the whole dependent tree and wipes production bookings.

**Fix:** in every config, `OnDelete(DeleteBehavior.Restrict)` for lookup/reference FKs. Keep `Cascade` only for true parent→detail (booking → lines, slot → slot_contract, wallet → ledger). Effort: **small** (config-only migration; `user_role` already does this correctly).

### 2. Shadow FK on `extra_booking.slot_id`
`Slot.ExtraBookings` is a collection navigation with no inverse property, so EF silently created a nullable `slot_id` column + index on `extra_booking` via shadow property. It is unused, always NULL, and anyone who later adds a real `SlotId` will fight the convention.

**Fix:** remove the `Slot.ExtraBookings` navigation (or make it explicit: `ExtraBooking.SlotId` + configured relationship). Drop the column + `ix_extra_booking_slot_id`. Effort: **small**.

### 3. `user_role` uniqueness defeated by NULL
`UNIQUE (user_id, role_id, facility_id)` — Postgres treats NULLs as distinct, so duplicate `(user, role, NULL)` "global role" rows are allowed. The same role can be granted twice and grant-check code that assumes uniqueness becomes non-deterministic.

**Fix:** `CREATE UNIQUE INDEX … NULLS NOT DISTINCT` (EF Core 9+: `HasIndex(...).IsUnique().HasIsNullDistinct(false)`), or a partial unique index where `facility_id IS NULL` plus one where it `IS NOT NULL`. Effort: **small**.

### 4. Booking money columns have no source of truth
`booking` stores `amount_paid`, `amount_outstanding`, `is_paid` but **no total**. Totals are hand-maintained across endpoints; a double-credit bug already happened (see the guard comment in `PaymentSucceededHandler` and idempotency handling in `PaymentResultHandler`). If extras/lines change or lines are deleted (the expiry job does exactly that), the numbers drift with no way to reconcile.

**Fix:**
- Add `total_amount` snapshot on `booking` (set once at creation).
- CHECK constraints: `amount_paid >= 0 AND amount_outstanding >= 0 AND amount_paid + amount_outstanding = total_amount` (for the relevant statuses).
- Derive `is_paid` (computed or projection) instead of a stored flag, or constrain it to match `amount_outstanding = 0`.

Effort: **medium** (backfill + constraint + endpoint changes).

### 5. No idempotency / concurrency protection on money rows
- `wallet_transaction.reference_id` is a free string with **no unique constraint** — the Wallet.md principle "an operation must never be applied more than once" is unenforceable today.
- `wallet_balance`, `wallet_voucher_grant`, `booking` have no concurrency tokens; concurrent redemptions/payments rely purely on transaction discipline in app code.

**Fix:**
- Unique index on `wallet_transaction (wallet_id, wallet_transaction_type_id, reference_id, wallet_transaction_status_id)` — or better, an explicit `idempotency_key` column with a unique index.
- Map Postgres `xmin` as a concurrency token on `wallet_balance` and `wallet_voucher_grant` (`builder.Property(x => x.Version).IsRowVersion()`), so concurrent draw-downs fail fast instead of over-drawing.
- CHECK constraints: `wallet_voucher_grant.amount_remaining BETWEEN 0 AND amount_granted`, `wallet_balance.balance >= 0`, `wallet_transaction.amount > 0`.

Effort: **small–medium**, must land before wallet/voucher runtime work.

### 6. Expiry job destroys booking lines
`ClearExpiredBookings` flips pending→expired and then **deletes the `slot_contract_booking` rows**. The booking keeps its money columns but loses what was booked — no audit trail, no re-price for reporting, and the deleted lines break any future refund/dispute flow.

**Fix:** stop deleting; expiry is already expressed by status. If the concern is capacity counting, the availability queries already filter by status. Add a partial index for the sweep (see #11). Effort: **small**.

## P1 — data-integrity gaps

### 7. Redundant denormalised parents
- `slot.facility_id` duplicates `resource.facility_id` (both nullable — a slot can also point at a facility that contradicts its resource).
- `extra.outlet_id` duplicates `facility.outlet_id`.

These are read-optimisation copies with nothing keeping them consistent; queries already mix both paths (booking create uses `slot.FacilityId`, extras check `extra.FacilityId`).

**Fix options:** (a) keep one canonical path + CHECK/FK consistency (e.g. `slot.facility_id` generated column or trigger-enforced equal to `resource.facility_id`), or (b) accept the denormalisation but make both `NOT NULL`-consistent via app invariant + tests, and document which column wins per query. A facility-less slot should not be representable. Effort: **small** (decision) + migration.

### 8. Missing FK: `slot_contract_booking.user_id`
Raw `Guid?` with no FK to `user` — typos/stale ids are invisible. Either make it a real FK (`SetNull`/`NoAction`) or drop it in favour of `booking.user_id` (they are always equal today — the player is the booking user).

**Fix:** add FK or remove the duplicated column. Effort: **small**.

### 9. No price snapshots on booking lines
`slot_contract_booking` and `extra_booking` reference `slot_contract`/`extra` for price. Change a price and every historical booking re-prices; refunds and reporting become wrong. The catalog should be mutable history; the line should be frozen.

**Fix:** add `price` (snapshot at booking time) to both line tables; backfill from current prices. Effort: **small–medium**.

### 10. `payment_booking` has no amount
M:N payment↔booking without a payload — when one payment covers multiple bookings (already supported by the schema) the allocation is unrecorded, so `amount_paid` per booking is unverifiable.

**Fix:** add `amount` to `payment_booking`; optionally CHECK that allocations per payment sum to `payment.amount`. Also consider adding `currency` to `payment` (currently hardcoded "ZAR" in code while `wallet.currency` exists). Effort: **small**.

### 11. Missing indexes for hot paths
- `slot (start_datetime)` — slot coverage job (`AnyAsync` on date range) and date-based availability scans are seq-scans today.
- `booking (booking_status_id, expires_at)` partial `WHERE booking_status_id = 1` — hourly expiry sweep.
- `wallet_voucher_grant (wallet_id, expiry_date)` — "live voucher balance" queries (FIFO by expiry).
- `payment (payment_status_id)` or `(payment_status_id, payment_status_date)` for provider reconciliation sweeps.

Effort: **small**.

### 12. Booking status has no history
Only the latest status + timestamp is kept. "When was it confirmed, who cancelled, why" is unrecoverable — the payment side already has `payment_log` as a pattern.

**Fix:** add an append-only `booking_status_log (booking_id, status_id, changed_at, changed_by, reason?)`, written in the same transaction as status changes (or via the audit interceptor). Effort: **small–medium**.

### 13. Voucher redemption audit table is missing (and needed)
Voucher.md lists `BookingVoucherApplication` as "to build" — but redemption decrements `wallet_voucher_grant.amount_remaining` in place, so without an application ledger a refund/expire/reversal cannot be reconstructed. Build it **before** redemption logic:

```
booking_voucher_application (id, booking_id, line_type (slot_contract|extra), line_id,
  wallet_voucher_grant_id, amount_applied, applied_at, sequence, reversed_by?)
```

**Fix:** add the table with FKs to booking + grant and CHECK `amount_applied > 0`. Effort: **small** (schema) — the runtime is separate.

### 14. User lifecycle is inconsistent
`user` delete cascades wallet, wallet transactions, grants, user_contracts — but fails on `booking.user_id` (NO ACTION) and `slot_contract_booking.user_id` (no FK). Half-deletes are impossible today by accident, not by policy.

**Fix:** decide retention policy: bookings should probably keep users (`NoAction` everywhere + soft-delete/deactivate users instead), or anonymise (`SET NULL` + snapshot name). Document it. Effort: **decision + small migration**.

## P2 — simplification & consistency

### 15. Pick ONE status/type representation
Currently three coexist: seeded lookup tables (`booking_status`, `payment_status`, `payment_type`, `wallet_transaction_*`), int enum columns (`voucher.redemption_kind`, `payment_provider_config.type`), and string enum columns. The lookup tables double-maintain a C# enum (seeded IDs must mirror enum values forever) and are the source of the cascade risk in #1.

**Recommendation:** for closed, code-switched-on sets (booking/payment/wallet status & type) use enum properties with `HasConversion<int>()` and drop the lookup tables — one source of truth, fewer joins, cascade risk gone. Keep real reference data (`validation`, `facility_type`, `outlet_type`, `voucher` catalog) as tables. Effort: **medium** (joins/DTOs/seed cleanup).

### 16. Dead weight — remove or wire up
- `booking_item` — mapped table, zero usage. Drop it.
- `slot_config` / `slot_config_type` — mapped, unused; they are the *intended* replacement for the hardcoded random slot generation in `SeedDbContext`. Either commit to that feature or drop and reintroduce with the feature.
- `site`, `file_meta`, `resource_slot_config` — unmapped dead entities; delete the classes.

Effort: **small**. Dead schema is documentation debt and migration noise.

### 17. `wallet_balance` 1:1 table → merge into `wallet`
A separate table for one cached column buys nothing (the ledger and balance are updated in the same transaction anyway) and costs a join on the hottest wallet read. Collapse `balance`/`updated_at` onto `wallet`. Effort: **small**.

### 18. Naming: `slot_contract` / `slot_contract_booking`
"SlotContract" reads as a join between Slot and Contract but is actually the priced bookable offer (9 holes vs 18 holes); "Contract" itself is the membership product. Every new developer pays this tax (the fix doc you're reading needed a paragraph for it). If renaming is ever affordable, do it before the surface grows: `contract → membership_plan` (or keep), `slot_contract → slot_offer`, `slot_contract_booking → booking_line`. Effort: **large** — schedule deliberately or accept the name.

### 19. Column naming & conventions drift
- `email_log` has PascalCase columns (`"Payload"`, `"Subject"`, `"Message"`) inside a snake_case DB; `payment_log` overrides its table name only. Remove `HasColumnName` overrides.
- `email_log` has no recipient/`to` column (recipients live inside the `payload` jsonb) and no `sent_at`; add them if email ops matter.
- `facility.is_active` is `bool?` while `outlet.is_active`/`resource.is_active` are `bool` — make it non-nullable with a default.
- Money columns are unbounded `numeric`; apply `HasPrecision(18, 2)` to money/price columns for intent and scale safety.
- `contract.frequency` (int, default 12) has unnamed units — rename/Document (months?), or model as interval.

Effort: **small, batchable**.

### 20. `user` is a reserved word
EF always quotes it, but hand-written SQL (the TickerQ `Sql` job executes arbitrary SQL; jobs use raw SQL) can silently hit the wrong thing (`FROM user` returns the current user in Postgres). Rename table to `app_user`/`users` while the DB is young, or ban unquoted raw SQL. Effort: **small migration now, painful later**.

### 21. Audit stamping via `SaveChangesAsync` override
Only the async override stamps `AuditableEntity`; sync `SaveChanges` bypasses it, `DateTime.UtcNow` is used directly (untestable), and soft-delete-style concerns are absent. Move to a `SaveChangesInterceptor` with `TimeProvider` per the EF guidelines. Also decide: nothing is soft-deleted today — if catalog rows (`extra`, `contract`) must keep history for bookings (see #9), make that explicit rather than relying on cascade deletes being avoided. Effort: **small**.

### 22. Voucher scoping conventions
- "No `voucher_facility` rows = valid everywhere" is a convention that makes queries awkward (`!Any() || Any(x == f)`) and cannot be distinguished from "misconfigured". Consider `voucher.is_global` or a sentinel, or at least an admin-time validation.
- A voucher can target *all* extras or *all* slot contracts at scoped facilities — there is no way to scope "burgers only, not carts". When the product needs it, add `voucher_extra`/`voucher_slot_contract` target link tables rather than overloading `is_extra`.

Effort: **defer until redemption work starts** (do #13 first).

### 23. Guest identity model
Guest bookings rely on `booking.user_id = null` + per-line `name/email/cellphone` duplicates. If guest marketing/comms matter, promote a lightweight `guest` record (or keyed identity per email) instead of free-text per line. Effort: **defer; revisit with product**.

## Suggested sequencing

1. **Migration 1 (safety):** #1 Restrict FKs, #2 shadow FK drop, #3 unique index fix, #6 stop deleting lines, #11 indexes, #16 drop dead tables.
2. **Migration 2 (money):** #4 booking totals + CHECKs, #5 idempotency + concurrency tokens + CHECKs, #10 payment_booking.amount, #9 price snapshots (+ backfills).
3. **Migration 3 (consistency):** #7 denormalised parents, #8 FK, #12 status log, #13 voucher application ledger, #17 merge wallet_balance, #19/20 naming.
4. **Deliberate projects:** #15 lookup→enum consolidation, #18 renames, #21 interceptor, #22–23 with product.

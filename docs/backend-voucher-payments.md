# Backend split and voucher payments

The entitlement-eligibility follow-up includes facility voucher administration UI and regenerated clients. Grant provenance remains a separate preceding change.

## Split payments

`POST /payment/initiate` accepts an optional `amount`. Omit it to pay the unreserved outstanding balance. Amounts must be positive, have at most two decimal places and not exceed that balance. Pending provider payments reserve their amounts until a success or failure callback arrives. Different providers can pay different parts of the same booking.

A successful payment is `Partial` (payment status ID 5) while the booking has an outstanding balance. Partial payments have already been credited: repeat callbacks do not credit them again. The booking remains pending and `isPaid` is false. Once fully settled, its payments become `Completed` and the booking becomes confirmed. Failed payments do not contribute to the balance. Bookings cannot be edited while payments are pending or after any amount has been paid.

`GET /payment/booking/{bookingId}` returns the owner's payment history, paid amount, outstanding amount, unreserved available amount and paid flag.

## Voucher payments

Payment type ID 4 is `Voucher`.

`GET /payment/booking/{bookingId}/vouchers` returns the authenticated owner's grants associated with a booked facility, including eligibility/rejection reasons, remaining units/credit, expiry, redemption kind, discount mode/value/cap, prospective payment value and entitlement targets. Grants for other facilities (or with no facility associations) are omitted. For bookings spanning multiple facilities, the list uses the union of booked facilities; each item's eligibility still requires its own facility intersection. Entitlement preview value is for one available unit; targets contain the individual unit prices and available quantities.

`POST /payment/voucher` accepts:

```json
{
  "bookingId": 123,
  "grantId": "grant-uuid",
  "slotContractBookingId": 456,
  "quantity": 1
}
```

- Round entitlements select a `slotContractBookingId`; extra entitlements select an `extraId` instead. Their payment value is the booked slot contract/extra unit price times quantity, capped at the unpaid eligible subtotal and unreserved balance. Redeemed units cannot be used twice.
- Percentage discounts apply to eligible unpaid slot/extra prices, with `maxDiscountAmount` respected. Fixed discounts use `discountValue`, capped to the eligible subtotal/balance. They consume one use, and each discount grant can be used only once per booking.
- Credit vouchers consume monetary credit equal to the payment value.
- Discounts and credits omit item IDs and use the default quantity of one.
- Voucher facilities explicitly restrict eligibility. `isExtra` distinguishes extra-only vouchers from slot-only vouchers. The wallet must belong to the booking owner, be active and use ZAR. Grants must have remaining balance and be valid now; booked times must fall within `[GrantedAt, ExpiryDate)`. Empty facility associations do not mean unrestricted eligibility. Source contract ownership, status and dates do not participate in redemption: cancellation, expiry or deletion of a source membership does not revoke an issued grant.
- Previously redeemed voucher value is excluded from the discountable subtotal. There is no payout of unused value when a discount/entitlement exceeds the balance.

The response contains transaction ID, applied value, payment status, updated paid/outstanding amounts and paid flag. Grant consumption, payment, payment-booking link, voucher audit record and booking amounts commit together. Booking/grant row locks prevent concurrent overpayment and double consumption.

Apply the payment migrations and `WalletVoucherGrantProvenance` before using these endpoints. No vouchers or grants are automatically issued on contract activation.

## Grant issuance and provenance

PR #52 contained no production issuance endpoint (only direct test fixture inserts). `POST /admin/facility/{FacilityId}/voucher/issue` is the explicit issuance flow, protected by the existing facility Manager policy. It accepts `walletId`, `voucherId`, positive `amount`, UTC `validFrom` and `expiryDate`, optional `sourceUserContractId`, `reason` (1000 characters) and `reference` (200 characters), and returns the new grant UUID. Non-credit amounts must be whole units; credit amounts permit two decimal places. The wallet must be active and use ZAR. For authorization isolation, the voucher must be associated exclusively with the manager's route facility.

Omit `sourceUserContractId` for standalone admin issuance. If supplied, the membership must belong to the wallet owner, be active/current and link to a contract granting this voucher at the route facility. The server derives `Contract` or `Admin` source from this validated association and assigning-user identity from authentication. There are no caller-controlled actor, action, audit timestamp or source-type fields. Purchase, gift and system source values are supported by the audit model for future trusted issuance flows, not exposed as arbitrary manager request values.

Each issuance saves the grant and a `WalletVoucherGrantAudit` together in one EF transaction. Audit entries store grant ID, action (`Issued`), UTC event timestamp, source type, optional assigning-user/source-membership identifiers, reason and reference. Identifier snapshots have no user/contract FK: source deletion preserves history. The grant FK is restrictive, so audited grants (and their wallets) cannot be hard-deleted through cascades. EF guards and a PostgreSQL trigger reject audit updates/deletes/truncation; issuance has a unique per-grant audit index. No revocation flow existed and none is introduced.

The migration creates and backfills an issuance audit for every legacy grant **before** removing its contract FK, index and column. It preserves `UserContractId` as a snapshot and uses `GrantedAt` as the only known legacy issuance timestamp. Assigning user, reason and reference remain null rather than inventing historical facts. Grant balances/dates and all payment/redemption tables are untouched. New issuance uses the actual current audit timestamp independently of `validFrom` (stored in `GrantedAt`).

Deploy with the API stopped or writes quiesced: old code requires the removed column. Back up first. Migration rollback is deliberately unsupported because standalone grants and deleted sources cannot be represented by the old mandatory contract FK; restore a pre-migration backup instead. Client regeneration for this provenance API was performed in the separate entitlement-eligibility follow-up.

## Entitlement item eligibility (separate follow-up)

`VoucherContract` and `VoucherExtra` are many-to-many **redemption** allowlists, independent of `ContractVoucher` (issuance) and audit source memberships. Round entitlements match `SlotContract.ContractId`, never `UserContractId`. Extra entitlements match `ExtraBooking.ExtraId`. Only the list corresponding to `isExtra` is relevant. Empty lists mean no eligible items; multiple IDs allow any matching item.

These lists affect **Entitlement only**. Discounts and credit ignore both lists entirely, including when empty. Their eligible subtotals continue to use facility associations, grant validity and booking items. Entitlement targets/subtotals require both the item allowlist and facility intersection in addition to ownership, wallet status/currency, validity and remaining balance. Preview filters targets; redemption recomputes eligibility against current database restrictions rather than trusting a prior preview. Accounting, row locks, pending reservations and duplicate-consumption protections are unchanged.

`GET /wallet/vouchers` lists all the authenticated user's currently available grants across facilities, without needing a booking. It excludes inactive/unsupported wallets, expired/future/exhausted grants, fractional entitlement/discount units, vouchers without facilities and unconfigured entitlements with no allowed IDs (and extra entitlements with no allowed extra in a permitted facility). Round availability does not require active contracts, memberships or scheduled slots; the actual booked slot contract/facility is checked at redemption. It returns grant/voucher metadata, balance, validity and facility/contract/extra IDs. Availability is not a guarantee that a particular booking is payable or has eligible unpaid items.

Facility managers can use `GET`/`POST /admin/facility/{FacilityId}/voucher` and `PUT /admin/facility/{FacilityId}/voucher/{Id}`. Create/update accept voucher metadata plus `contractIds` and `extraIds` arrays; duplicates are normalized. Selected IDs must belong to the route facility. Round entitlements accept contracts only; extra entitlements accept extras only. Empty arrays are permitted intentionally (no entitlement redemption). Multi-facility vouchers are visible where associated but read-only to single-facility managers to prevent cross-facility changes. New vouchers are associated with exactly the route facility. The admin Vouchers page reuses the existing table, dialog, checkbox and TanStack form components and loads selection options only from that facility.

**Migration `VoucherItemEligibility`:** legacy vouchers start with explicitly empty item lists. There is no safe historical item eligibility to infer; issuance associations must not be interpreted as redemption permissions. The migration emits a notice for existing entitlements. Operators must configure their contract/extra lists before they can be redeemed. Discounts/credits and all grant balances, validity dates, provenance and payment history are untouched. Contract/extra deletion is restrictive while allowlisted; remove the association explicitly first. Apply with the API stopped to avoid old code redeeming without the new checks; rolling back removes configured lists and old code restores facility-only semantics.

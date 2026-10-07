# Backend split and voucher payments

Frontend work and API client regeneration are deferred to the next step.

## Split payments

`POST /payment/initiate` accepts an optional `amount`. Omit it to pay the unreserved outstanding balance. Amounts must be positive, have at most two decimal places and not exceed that balance. Pending provider payments reserve their amounts until a success or failure callback arrives. Different providers can pay different parts of the same booking.

A successful payment is `Partial` (payment status ID 5) while the booking has an outstanding balance. Partial payments have already been credited: repeat callbacks do not credit them again. The booking remains pending and `isPaid` is false. Once fully settled, its payments become `Completed` and the booking becomes confirmed. Failed payments do not contribute to the balance. Bookings cannot be edited while payments are pending or after any amount has been paid.

`GET /payment/booking/{bookingId}` returns the owner's payment history, paid amount, outstanding amount, unreserved available amount and paid flag.

## Voucher payments

Payment type ID 4 is `Voucher`.

`GET /payment/booking/{bookingId}/vouchers` returns all of the authenticated owner's voucher grants, including eligibility/rejection reasons, remaining units/credit, expiry, redemption kind, discount mode/value/cap, prospective payment value and entitlement targets. Entitlement preview value is for one available unit; targets contain the individual unit prices and available quantities.

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

PR #52 contained no production issuance endpoint (only direct test fixture inserts). `POST /admin/facility/{FacilityId}/voucher/issue` is the explicit issuance flow, protected by the existing facility Manager policy. It accepts `recipient` (the full email address or phone number), `voucherId`, positive `amount`, UTC `validFrom` and `expiryDate`, optional `sourceUserContractId`, `reason` (1000 characters) and `reference` (200 characters), and returns the new grant UUID. Recipient matching trims surrounding whitespace and ignores email casing; phone numbers must match the stored value. There is no user search or partial matching. Missing or ambiguous recipients return a validation message without creating a wallet or grant. Non-credit amounts must be whole units; credit amounts permit two decimal places. An existing wallet must be active and use ZAR; otherwise a new active ZAR wallet is created for the recipient. For authorization isolation, the voucher must be associated exclusively with the manager's route facility.

Omit `sourceUserContractId` for standalone admin issuance. If supplied, the membership must belong to the wallet owner, be active/current and link to a contract granting this voucher at the route facility. The server derives `Contract` or `Admin` source from this validated association and assigning-user identity from authentication. There are no caller-controlled actor, action, audit timestamp or source-type fields. Purchase, gift and system source values are supported by the audit model for future trusted issuance flows, not exposed as arbitrary manager request values.

Facility admins can use **Send voucher** from the voucher admin page, enter an email or phone number, quantity/credit amount and expiry, and send directly to the recipient's wallet. Each issuance saves any new wallet, the grant and a `WalletVoucherGrantAudit` together in one EF transaction. Audit entries store grant ID, action (`Issued`), UTC event timestamp, source type, optional assigning-user/source-membership identifiers, reason and reference. Identifier snapshots have no user/contract FK: source deletion preserves history. The grant FK is restrictive, so audited grants (and their wallets) cannot be hard-deleted through cascades. EF guards and a PostgreSQL trigger reject audit updates/deletes/truncation; issuance has a unique per-grant audit index. No revocation flow existed and none is introduced.

The migration creates and backfills an issuance audit for every legacy grant **before** removing its contract FK, index and column. It preserves `UserContractId` as a snapshot and uses `GrantedAt` as the only known legacy issuance timestamp. Assigning user, reason and reference remain null rather than inventing historical facts. Grant balances/dates and all payment/redemption tables are untouched. New issuance uses the actual current audit timestamp independently of `validFrom` (stored in `GrantedAt`).

Deploy with the API stopped or writes quiesced: old code requires the removed column. Back up first. Migration rollback is deliberately unsupported because standalone grants and deleted sources cannot be represented by the old mandatory contract FK; restore a pre-migration backup instead. Frontend work/client regeneration remains deferred.

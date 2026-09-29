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
- Voucher facilities explicitly restrict eligibility. `isExtra` distinguishes extra-only vouchers from slot-only vouchers. Wallet and contract must be active, dates must be valid, and the booked time must fall within the contract and grant validity period. Empty facility associations do not mean unrestricted eligibility.
- Previously redeemed voucher value is excluded from the discountable subtotal. There is no payout of unused value when a discount/entitlement exceeds the balance.

The response contains transaction ID, applied value, payment status, updated paid/outstanding amounts and paid flag. Grant consumption, payment, payment-booking link, voucher audit record and booking amounts commit together. Booking/grant row locks prevent concurrent overpayment and double consumption.

Apply the three new migrations before using these endpoints. No vouchers or grants are automatically issued by this change; existing contract/wallet issuance remains separate.

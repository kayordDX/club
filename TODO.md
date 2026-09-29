Backend implementation completed for the sections below. Frontend payment amount selection, outstanding-payment display and voucher cards remain to do. See `docs/backend-voucher-payments.md` for the API contract.

### Add new payment type Voucher

This is a payment type that should allow you to pay with voucers.

### Allow us to split the payments

Because of voucers we should be able to pay part of the payment with voucher or discount and the rest with the other payment methods.
But we want to also make it possible to choose the amount if you really want to and split payment or pay some part with one provider and the rest with another.
Allow to choose the amount when making a payment
Show what has been paid and what is outstanding

### Voucher Payment

Payment with the voucher payment type.
Get all user vouchers
Check which could be valid for this transaction
List vouchers in cards that could be used for this transaction.
This should list the different types and display them in different ways.
Example show free golf rounds with count to show number of free rounds.
Show discount percentage or amounts in other cards.
This should insert into the same payment tables just with the voucher type to show it was used.
It should get the payment value.
If it is entitlement it should get price from what that unit would have cost without the voucher.
If it is Percentage voucher discount mode it should calculate price from percentage. The max_discount_amount should be taken into concideration as well.
If it is Fixed amount it should use that amount.

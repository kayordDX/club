# Voucher System

How vouchers work in the wallet. Vouchers are redeemable credits issued by **contracts** — a free round of golf, a burger, a golf-cart rental, 10% off, or R-value credit. Members accrue them and spend them at checkout.

For the **money** side of the wallet (ZAR balance, top-ups, transaction ledger), see [`Wallet.md`](./Wallet.md). This document covers vouchers only; the two tracks are independent but live on the same `Wallet`.

> **Status:** the data model below is implemented (`Club.Api/Entities/`) and migrated. The runtime behaviour marked _(to build)_ is not yet wired.

---

## TL;DR

- A **Voucher** is a catalog definition (e.g. "Free Round of Golf", "10% off", "R500 Golf Credit").
- Each voucher has a **`RedemptionKind`** — `Entitlement`, `Credit`, or `Discount` — that decides how it affects a booking.
- A **Contract** grants an **amount** of each voucher (`ContractVoucher`).
- When a member activates a contract, those amounts are credited to their wallet as **`WalletVoucherGrant`** rows — an append-only ledger with a per-batch remaining balance and expiry.
- At checkout, vouchers are applied **per line** in a waterfall: **discount reduces the price → entitlement/credit/money settle the rest**. Redemption is **amount-based draw-down**, not "one voucher per line".

---

## Core concepts

### `Voucher` — the catalog definition (`Entities/Voucher.cs`)
Defined once by an admin.

| Field | Type | Notes |
| --- | --- | --- |
| `Name`, `Description` | string | e.g. "Free Round of Golf" |
| `IsExtra` | bool | **Target** — what it redeems against: `false` → a `SlotContract` (a bookable facility service, e.g. a round); `true` → an `Extra` (an add-on, e.g. burger / golf cart) |
| `RedemptionKind` | enum | **Effect** — `Entitlement` \| `Credit` \| `Discount` (default `Entitlement`) |
| `DiscountMode` | enum? | `Percentage` \| `FixedAmount` — set only when `Discount` |
| `DiscountValue` | decimal? | `10` (=10%) or `50` (=R50) — set only when `Discount` |
| `MaxDiscountAmount` | decimal? | cap, e.g. "10% off up to R100" — set only when `Discount` |

`IsExtra` (the *target*) and `RedemptionKind` (the *effect*) are **orthogonal** — e.g. "10% off a round" = `Discount` + `IsExtra=false`; "R20 off a burger" = `Discount` + `IsExtra=true`.

### `VoucherFacility` — facility scoping (`Entities/VoucherFacility.cs`)
Many-to-many link between a voucher and the facilities where it's valid.
- No rows → valid at **all** facilities.
- Linked to two facilities → redeemable **only** at those.
This is how **credit** is facility-scoped.

### `ContractVoucher` — what a contract grants (`Entities/ContractVoucher.cs`)
`Contract × Voucher × Amount` (decimal). The `Amount` is interpreted by the voucher's `RedemptionKind`:

| Kind | `Amount` means | Example |
| --- | --- | --- |
| Entitlement | number of units | `10` = 10 rounds |
| Credit | R-value | `500` = R500 |
| Discount | number of uses | `2` = two "10% off" uses |

### `WalletVoucherGrant` — the held balance (`Entities/WalletVoucherGrant.cs`)
Append-only ledger row created when a contract is activated. One row **per batch**:

| Field | Meaning |
| --- | --- |
| `WalletId` | the member's wallet |
| `UserContractId` | the contract activation that issued it (full audit trail) |
| `VoucherId` | which voucher |
| `AmountGranted` | original amount |
| `AmountRemaining` | current spendable balance (drawn down on redemption) |
| `GrantedAt` | when credited |
| `ExpiryDate` | each batch has its own expiry |

Multiple grants of the same voucher **coexist** — e.g. a January batch expiring in December and a March batch expiring next March. There is no single "balance per voucher type" row; the live balance is the sum of non-expired grants' `AmountRemaining`.

### Money track (summary — see `Wallet.md`)
`WalletBalance` (1:1, current ZAR balance) + `WalletTransaction` (append-only ledger: signed `Amount`, `Credit`/`Debit` type, status, `ReferenceId`). Money movements stay on `WalletTransaction`; **voucher movements stay on the grant ledger** — the two never mix.

---

## The three redemption kinds

| Kind | Effect on a booking line | "Amount" means | Consumed per use | Example |
| --- | --- | --- | --- | --- |
| **Entitlement** | Covers a whole matching item (the line becomes free) | units | −1 unit | "Free round of golf" |
| **Credit** | R-amount applied toward the total, partial | R | −R applied | "R500 golf credit" |
| **Discount** | Reduces the line price by a rule **before payment** | uses (count) | −1 use | "10% off a round" |

A **Discount** is a *price modifier* (it changes what's owed); Entitlement / Credit / Money are *payments* (they settle what's owed). That distinction is why discount is applied first in the waterfall.

**Discount math**
- `Percentage`: `reduction = price × (DiscountValue / 100)`, then capped at `MaxDiscountAmount` if set.
  - 10% off a R200 round → R20 off.
  - "10% off, max R100" on a R2000 round → R100 off.
- `FixedAmount`: `reduction = DiscountValue`, never more than the line price.
  - R50 off a R200 round → R50 off.

---

## Lifecycle

1. **Define** — admin creates `Voucher`(s) and scopes them to facilities via `VoucherFacility`.
2. **Compose contract** — a `Contract` lists its `ContractVoucher` allowances (which vouchers, how much each). The contract itself is scoped to facilities via `ContractFacility`.
3. **Activate** — a member buys the contract → a `UserContract` is created → the system **mints one `WalletVoucherGrant` per `ContractVoucher`** into the member's wallet: `Amount` → `AmountGranted` = `AmountRemaining`, and `ExpiryDate` from the contract terms. _(Minting is runtime behaviour to build on top of this schema.)_
4. **Hold** — grants live in the wallet, each with its own remaining balance and expiry.
5. **Redeem** — at checkout, apply vouchers to booking lines (see waterfall). Each application decrements `AmountRemaining` and is recorded for audit.
6. **Expire / refund** — a grant past its `ExpiryDate` is unspendable; a refund credits units/R back onto the grant.

---

## Checkout waterfall

Applied **per booking line** (a `SlotContractBooking` or `ExtraBooking`), starting from the line price `P`:

1. **Discount** — if a matching discount voucher is applied: `P1 = P − discount` (record the reduction, consume 1 use).
2. **Entitlement** — if a matching entitlement voucher is applied and the line matches its voucher target: the line is covered, consume 1 unit, remainder = 0.
3. **Settle** the remainder (`P1`, or `P` if no discount/entitlement) with **Credit** voucher(s) (−R) and/or **Money** (debit `WalletBalance`).

**Rules**
- **Amount-based draw-down** — you can take any amount from a grant's `AmountRemaining`. A booking for 4 rounds consumes 4 units from a 100-round grant. One line can draw from multiple grants (typically FIFO by `ExpiryDate`).
- **Scoping** — a voucher only applies at its `VoucherFacility` facilities (credit especially).
- **Stacking** — discount applies before settlement; entitlement/credit/money then settle what's left. The exact "what may combine on one line" policy is a product decision; the default allows the sequence above.

> Voucher redemption follows the same principles as the money ledger (see `Wallet.md`): immutable ledger, deterministic remaining balance, explicit application records, idempotent operations, and concurrency-safe updates (decrement `AmountRemaining` inside a DB transaction).

---

## Worked examples

**1. "100 rounds" contract**
`Voucher { Name: "Golf Round", IsExtra: false, RedemptionKind: Entitlement }`
`ContractVoucher { Amount: 100 }` → on activation, a grant with `AmountRemaining: 100`. Each round booked consumes 1; depleted after 100.

**2. "10% off a round, max R100"**
`Voucher { Name: "10% off golf", IsExtra: false, RedemptionKind: Discount, DiscountMode: Percentage, DiscountValue: 10, MaxDiscountAmount: 100 }`, scoped to the golf facility via `VoucherFacility`.
`ContractVoucher { Amount: 2 }` → grant with 2 uses. Booking a R200 round → −R20, 1 use. Booking a R2000 round → −R100 (capped), 1 use.

**3. "R500 golf credit"**
`Voucher { Name: "Golf Credit", RedemptionKind: Credit }`, scoped to golf facilities.
`ContractVoucher { Amount: 500 }` → grant `AmountRemaining: R500`. A R180 round → −R180, leaving R320. Partial — no whole-unit requirement.

**4. "Free burger"**
`Voucher { Name: "Burger", IsExtra: true, RedemptionKind: Entitlement }`
`ContractVoucher { Amount: 2 }` → 2 burgers. Redeems against the `Extra` "Burger"; consumes 1 per burger on a booking.

---

## Entity map

```
Voucher (catalog) ──< VoucherFacility >── Facility
        │
        ├──< ContractVoucher >── Contract ──< ContractFacility >── Facility
        │
        └──< WalletVoucherGrant >── Wallet ──── User
                                  └── UserContract ──< Contract
```

- `Contract` → `ContractFacility` → `Facility` : which facilities the contract covers.
- `Contract` → `ContractVoucher` → `Voucher` : what the contract grants.
- `Contract` → `UserContract` → `WalletVoucherGrant` → `Voucher` : a member's held balance, traced to the activation.

---

## Open / to build

The schema is in place; these are runtime pieces still to implement:

- **Minting service** — create `WalletVoucherGrant` rows when a `UserContract` is created.
- **`BookingVoucherApplication`** — audit table recording which grant funded which booking line, the amount applied, and the sequence (needed for refunds/reversals and reporting).
- **Redemption logic** — the waterfall + `AmountRemaining` decrement at checkout.
- **Expiry handling** — a job/rule marking grants unspendable past `ExpiryDate`.
- **Stacking policy** — formalise which voucher combinations are allowed per line.

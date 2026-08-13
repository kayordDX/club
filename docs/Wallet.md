# Wallet

The wallet holds two independent kinds of value:
- **Money** — a ZAR balance backed by an immutable transaction ledger (this document).
- **Vouchers** — redeemable entitlements / credits / discounts issued by contracts (see [`voucher.md`](./voucher.md)).

> **Status:** the money-side data model below is implemented (`Club.Api/Entities/`) and migrated. Items marked _future_ are not yet built.

## Open questions

- [ ] Should the wallet be a separate project/service, or stay inside `Club.Api`?
- [ ] Do we need a `LockedBalance` (funds held for in-flight bookings) separate from available balance?
- [ ] Do we add `Created` / `Reversed` states and a reversal/audit trail?
- [ ] Do we need multiple currencies and regulatory constraints?

## Principles

- Backed by **immutable transactions** — not just a balance column. The balance is derived from the ledger.
- **Deterministic balance** = sum of completed `Credit` − `Debit` transactions. `WalletBalance` is a cache of that.
- Transaction **type** (`Credit`/`Debit`) and **status** are explicit lookup values.
- **Idempotent** operations — an operation must never be applied more than once.
- **Concurrency-safe** — balance/ledger writes run inside a DB transaction (optimistic or row-level locking).

## Architecture

The wallet currently lives inside `Club.Api` (entities in `Club.Api/Entities/Wallet*.cs`, accessed via `AppDbContext`). There is **no separate wallet service** yet — that remains an open question.

```mermaid
api(Club.Api) --> wallet(Wallet / AppDbContext) --> transactions(Transaction ledger) --> balance(Balance cache)
```

## Data model (current)

### `Wallet` — `Entities/Wallet.cs`

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | Guid | PK |
| `UserId` | Guid | 1:1 with `User` (unique index + FK, cascade) |
| `IsActive` | bool | soft-active flag (default `true`) — there is no status enum |
| `Currency` | string | `"ZAR"` |

Navigations: `Balance` (1:1 `WalletBalance`), `Transactions` (`WalletTransaction`), `VoucherGrants` (`WalletVoucherGrant`). `Wallet` is **not** auditable — it has no `Created`/`LastModified` columns.

### `WalletTransaction` — `Entities/WalletTransaction.cs`

| Field | Type | Notes |
| --- | --- | --- |
| `Id` | Guid | PK |
| `WalletId` | Guid | FK → `Wallet` (cascade) |
| `Amount` | decimal | the value; direction is given by `WalletTransactionType` |
| `WalletTransactionTypeId` | int | FK → `WalletTransactionType` (`Credit` / `Debit`) |
| `WalletTransactionStatusId` | int | FK → `WalletTransactionStatus` (`Pending` / `Completed` / `Failed` / `Refunded`) |
| `ReferenceId` | string | id of the source (booking, payment, top-up, …) |
| `CreatedAt` | DateTime | |

### `WalletBalance` — `Entities/WalletBalance.cs`

| Field | Type | Notes |
| --- | --- | --- |
| `WalletId` | Guid | PK + FK → `Wallet` (1:1, cascade) |
| `Balance` | decimal | current money balance (cache of the ledger) |
| `UpdatedAt` | DateTime | |

There is a single `Balance` — no separate available/locked split yet.

### Lookups (seeded)

- `WalletTransactionType` — `Credit` (1), `Debit` (2).
- `WalletTransactionStatus` — `Pending` (1), `Completed` (2), `Failed` (3), `Refunded` (4).

Enums: `Club.Api/Common/Enums/WalletTransactionTypeEnum.cs`, `WalletTransactionStatusEnum.cs`.

## Determining the balance

```
balance = Σ Amount (status = Completed, type = Credit)
        − Σ Amount (status = Completed, type = Debit)
```

`WalletBalance.Balance` should equal this. The **ledger is the source of truth**; the balance row is a cache for fast reads and must be kept in sync within the same transaction that writes the ledger row.

## Future / not yet implemented

- **`LockedBalance`** — funds reserved for in-flight bookings, separate from available. Not modelled yet (single `Balance`).
- **Extra states** — `Created` (pre-pending) and `Reversed` (explicit reversal, distinct from `Refunded`), if reversals/audits are needed.
- **Reversal/audit trail** — linking a reversing transaction to its original (e.g. a `ReversalOfId`).
- **Separate wallet service/project.**
- **Multi-currency.**

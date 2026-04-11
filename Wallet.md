# Wallet

Wallet planning

## Questions

- [ ] Should wallet be a seperate project and service?
- [ ] Do we add advanced features, multiple currencies, trasaction states, reversals audits, regulatory constraints?

## Principles
- Backed by immutable transactions (Not just a balance column)
- Deterministic balance calculation
- Transaction states must be explicit
  - Created
  - Pending
  - Completed
  - Failed
  - Reversed
- Idempotent operations - should never be applied more than once
- Find a way to handle concurrency safely. Also run in transactions.

## Architecture

```mermaid
api --> wallet(Wallet Service) --> transactions(Transaction ledger) --> balance(Balance calculation)
```

## Data Model

### Wallet Table
- Id
- UserId
- Currency
- Status
- CreatedAt

### Transaction Table
- Id
- WalletId
- Amount
- Type (Credit/Debit)
- Status (Created/Pending/Completed/Failed/Reversed)
- CreatedAt
- ReferenceId

### Balance Table
- WalletId
- AvailableBalance
- LockedBalance
- UpdatedAt

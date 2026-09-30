# Frontend browser tests

`pnpm test:e2e` runs the existing tests against the Aspire-started application (see the root `AGENTS.md`).

`pnpm test:e2e:payments` runs the authenticated booking payment scenarios independently of Aspire:

- Starts an isolated SvelteKit dev server on `127.0.0.1:5174` and a deterministic API fixture on `127.0.0.1:5194`. Both ports must be free.
- Uses an encrypted session cookie with a test-only secret, exercising the real protected layouts, remote functions and server-side bearer-token proxy. The fixture rejects API requests without the expected token.
- Covers entitlement targets and quantities, capped discounts, credits, split-provider payments, pending reservations, rejected selections, duplicate submission protection, and refresh failures.
- Does not mock browser payment requests or add a production authentication bypass. API fixtures and scenario controls exist only in the test process.

`pnpm test` runs unit/component tests, these isolated payment scenarios, then the Aspire-dependent suite.

These tests verify frontend behaviour against the documented API contract, **not** real Keycloak login, database voucher eligibility/locking/consumption, or live provider callbacks. Those require the running stack and backend integration tests. Fixture callback controls simulate provider outcomes; they do not contact payment providers. Grant issuance, revocation and refunds are not covered or implemented here.

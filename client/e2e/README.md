# Frontend browser tests

`pnpm test:e2e` runs the existing tests against the Aspire-started application (see the root `AGENTS.md`).

`pnpm test:e2e:payments` runs the authenticated booking payment scenarios independently of Aspire:

- Starts an isolated SvelteKit dev server on `127.0.0.1:5174` and a deterministic API fixture on `127.0.0.1:5194`. Both ports must be free.
- Uses an encrypted session cookie with a test-only secret, exercising the real protected layouts, remote functions and server-side bearer-token proxy. The fixture rejects API requests without the expected token.
- Covers entitlement targets and quantities, capped discounts, credits, split-provider payments, pending reservations, rejected selections, duplicate submission protection, and refresh failures.
- Covers admin/customer success, failure and cancellation returns; transaction/booking association and safe internal admin navigation; copied customer URLs requiring sign-in and owner access.
- Exercises the combined admin schedule → customer selection → Confirmed unpaid creation → edit/move → checkout → admin return journey, with customer visibility and payment regression checks.
- Does not mock browser payment requests or add a production authentication bypass. API fixtures and scenario controls exist only in the test process.

The client PR job runs this isolated suite with Chromium; failures include an HTML report, retained trace, screenshot and runtime diagnostics.

All isolated specs import `test`/`expect` from `./fixtures/payment-test`. Its automatic fixture fails on unhandled browser errors (including additional pages), server error-channel output and API-fixture exceptions. Warnings and ordinary browser console messages are diagnostic context, not failures; handled negative HTTP responses remain valid. Listeners stay active through context shutdown, and suite teardown checks for remaining server errors. Error records survive scenario resets and are consumed between tests, so a worker restart cannot erase them. Vite crashes keep the fixture diagnostics service alive; collection failures still attach browser context and the retained server journal. No runtime-error allowlist is installed.

The fixture supplies OIDC discovery and a sign-in-required authorization endpoint, not a login/token exchange. This keeps unauthenticated redirects deterministic without Keycloak or a production auth bypass. `pnpm test:e2e:diagnostics` verifies that deliberately injected browser/rejection/additional-page/fixture/context-shutdown errors and a Vite crash fail otherwise-passing tests, while handled failures and warnings pass; it also tests server error capture. Unexpected probe-verification failures retain the child report, traces, screenshots and runner logs under `test-results/diagnostics` for CI upload.

The `Batch has scheduled effects` invariant reported in #90 was fixed by [PR #91](https://github.com/kayordDX/club/pull/91): `OutletBreadcrumbs.svelte` now uses a synchronous query-backed render instead of an awaited remote-derived breadcrumb during hydration. Customer/admin hydration and refresh/return journeys remain covered by the shared guard. Existing environment/proxy-identity warnings are not unhandled errors and remain visible.

`pnpm test` runs unit/component tests, these isolated payment scenarios, then the Aspire-dependent suite.

These tests verify frontend behaviour against the documented API contract, **not** real Keycloak login, database voucher eligibility/locking/consumption, or live provider callbacks. Those require the running stack and backend integration tests. Fixture callback controls simulate provider outcomes; they do not contact payment providers. Grant issuance, revocation and refunds are not covered or implemented here.

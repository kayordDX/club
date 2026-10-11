# Admin facility booking management

## Status

Scope and testing seams confirmed by the user. Published implementation spec: [GitHub issue #81](https://github.com/kayordDX/club/issues/81), labelled `ready-for-agent`. Application implementation has not started.

## Goal

Complete facility booking management using shared customer booking UI while keeping admins within facility admin navigation. Facility configuration redesign is excluded unless necessary for this workflow.

## Agreed behaviour

### Creation and ownership

- Admins select an existing registered customer through customer search.
- The selected customer is the booking owner; the admin is the acting admin.
- Admin-created bookings default to Confirmed, including when unpaid.
- Pending is an explicit alternative for temporary holds and uses the normal expiry window.
- Confirmed bookings do not expire as temporary holds.
- Contracts and prices use the customer's eligibility, never the acting admin's benefits.

### Facility workspace and shared screens

- Add a single-day facility schedule with date navigation, available/full slots, and direct access to each slot's bookings.
- Retain the searchable booking list alongside the schedule.
- Reuse customer availability and booking UI through shared components rather than routing admins into My bookings.
- Keep creation, details, editing, and checkout in admin context, with a clear return to the facility's bookings.

### Booking actions

- View details, edit players and extras, cancel, confirm, restore, and move bookings.
- Moves are limited to another available slot within the same facility.
- Moves retain players and extras where valid, preserve booking status, and use destination prices and owner eligibility.
- Preview changed prices before saving and require correction of incompatible players or extras.
- New or changed contracts must satisfy customer eligibility. Unchanged existing contracts may remain when eligibility has subsequently changed.
- Admin editing may bypass customer ownership, booking status, and expiry restrictions, but not capacity, facility boundaries, customer eligibility for new benefits, or payment integrity.
- Restore cancelled/expired bookings only after rechecking capacity; restoration to Pending receives a fresh normal expiry window.
- Preserve player details on cancellation so restoration remains possible.

### Payments

- Confirmation and payment are separate: a Confirmed booking may be unpaid.
- Admins can view payment history and initiate supported checkout for Pending or Confirmed bookings with an available outstanding balance.
- Cancelled and expired bookings must be restored before checkout.
- Reject changes whose total is below amounts already paid or reserved by pending payments; do not automatically refund.
- Admins cannot manually mark a booking paid or spend the customer's vouchers.
- Admins can copy the existing authenticated customer booking/payment URL for manual handoff. Do not introduce an anonymous payment link.

### Access and accountability

- Use existing facility Manager permissions, enforced on the server.
- Customer search supports selecting an owner without exposing unrelated bookings or financial information.
- Record the acting admin and time for creation, edits, moves, and status changes.
- No dedicated audit-history screen in this release.
- Leave customer notification behaviour unchanged; customers see owned bookings through existing customer screens.

## Exclusions

- Guest customers and account creation.
- Cross-facility transfers.
- Refunds and manual payment recording.
- Spending customer vouchers as an admin.
- New email/SMS workflows.
- Week/month schedules, drag-and-drop, and bulk actions.
- New role hierarchy and a dedicated audit-history screen.

## Existing implementation anchors

- Admin booking endpoints: `Club.Api/Features/Admin/Booking/`.
- Customer booking endpoints: `Club.Api/Features/Booking/`.
- Payment initiation: `Club.Api/Features/Payment/Initiate/Endpoint.cs`.
- Admin booking routes: `client/src/routes/(protected)/outlet/[slug]/[id]/admin/bookings/`.
- Shared booking components: `client/src/lib/components/booking/`.
- Customer availability/slot entry points: `client/src/routes/(pages)/outlet/[slug]/[id]/facility/` and `client/src/routes/(protected)/outlet/[slug]/[id]/slot/[slot]/`.
- Vocabulary: `GLOSSARY.md`.

## Known gaps from initial inspection

- Customer creation assigns ownership to the signed-in user; admin creation needs explicit customer ownership.
- Existing admin edit UI uses the current slot rather than supporting a move.
- Admin status changes currently do not check capacity before restoring reservations.
- Payment initiation currently accepts only Pending bookings.
- Some payment operations are owner-only; admin access must be separately authorised, not achieved by weakening customer access checks.

## Reusable seams: inspection findings

### Shared frontend modules

- The facility booking page already has day navigation and switches between public and admin slot queries. Its route-local Slots, Slot, and AdminSlot modules are the extraction starting point for a shared daily schedule under `$lib/`; the facility information page itself is not the availability screen.
- AdminSlot already displays player names/statuses and links to admin booking details, but its Book action enters customer creation and therefore creates a booking owned by the acting admin. Replace that route action with admin creation rather than duplicating the schedule.
- BookingDetails and BookingStatusControl already serve admin details. Keep these shared presentation modules and let route adapters supply authorised data and actions.
- BookingDetailsForm is already shared between customer creation/editing and admin editing. However, it fetches contracts using the signed-in actor and captures slot-dependent queries/options at initialisation. Its interface needs owner-aware booking options and deliberate reload/reset behaviour when the destination changes. Retain drafts during destination selection, rather than resetting them unintentionally.
- BookingPayments mixes payment history, checkout, voucher queries/redemption, and customer-specific editability. Share the history/amount/provider UI, but supply context-specific data/actions and exclude voucher querying and redemption entirely from the admin adapter. Hiding voucher markup alone is insufficient.
- Existing payment handoff stores only booking ID by transaction ID. Preserve the return context as well so provider results lead back to admin details when checkout began there.

### Backend modules and interfaces

- Existing admin booking HTTP endpoints are the main behavioural test seam. Extend them and add missing admin creation/lookup/options/payment-history interfaces with facility Manager authorisation rather than broadening customer access.
- Admin slot listing already provides daily capacity plus player/status summaries; reuse it for the management schedule.
- ContractEligibility already captures public-contract or dated-membership eligibility. Reuse it with active memberships for the selected owner, removing acting-manager bypass from the new owner-aware path. Keep existing unchanged contracts available during edits.
- Member lookup already searches local users and falls back to Keycloak, but its endpoint is contract-specific. Reuse the lookup logic behind a booking-specific interface. Existing member-add logic demonstrates syncing an existing Keycloak account locally; this is not account creation.
- Creation, customer editing, and admin editing duplicate booking mutation logic. A shared booking mutation module should own capacity locking/counting, contract/extras validation, repricing, and payment floors, with explicit customer/admin policy differences kept in authorised adapters. Do not build a generic CRUD abstraction.
- BookingPayments already provides booking-row locking and payment settlement helpers. Preserve that locking seam for edits, restoration, and checkout. Its available-balance helper currently returns outstanding balance without subtracting pending reservations; verify and test concurrent payment attempts against the agreed payment-integrity rule.
- AuditableEntity records creator and latest modifier, but not an action history. A minimal persistent booking action history is needed to retain every agreed admin action, recorded in the same transaction as the mutation.

### Correctness/security findings to include in the spec

- Customer booking status mutation checks authentication but does not check ownership. Fix and regression-test this as part of safe reuse; do not assume all existing customer actions are owner-protected.
- Capacity queries exclude Cancelled and expired Pending bookings, but not explicitly stored Expired status. Define a single active-reservation rule and apply it consistently to schedule counts, creation, edits, and restoration.
- Restoration currently updates status without transactional capacity validation.
- Contract options currently grant facility managers access to every contract, conflicting with the agreed owner-eligibility rule for on-behalf-of creation and changed contracts.

### Recommended testing seams

- **Primary:** authenticated HTTP integration tests against the existing booking, slot, and payment endpoints with real PostgreSQL. Test ownership, facility access, eligibility, capacity, balances, action history, and races through externally observable behaviour, not shared-module internals.
- **Frontend:** shared-module browser/component tests for customer selection, destination changes, retained drafts, price previews, and absence of admin voucher operations. Use the existing booking details/status/payment tests as prior art.
- **Journey:** a small number of browser flows covering schedule → admin creation → management → checkout return, plus customer visibility and a customer-flow regression. Existing payment E2E tests provide prior art.

The user approved these implementation/testing seams, which are captured in GitHub issue #81. No application modules have been changed.

## Implementation sequence

1. **Verify reusable seams and contracts**
   - Inspect customer lookup, availability, contract eligibility, payment access, and existing audit facilities.
   - Map which components can be shared without importing across route trees.
   - Define focused API/UI contracts and acceptance tests before changing application code.
2. **Backend booking management**
   - Add facility-authorised customer lookup and creation with explicit owner and initial status.
   - Support moves, eligibility rules, safe restoration, and acting-admin records.
   - Preserve transactional capacity and payment integrity checks across creation, edits, and status changes.
3. **Admin workspace and shared UI**
   - Add the daily schedule and admin create flow.
   - Extend shared booking details/forms for moves and admin context.
   - Keep list, schedule, and details refreshed after actions.
4. **Payment access and handoff**
   - Add authorised admin payment-history access.
   - Support checkout on Confirmed unpaid bookings without changing status.
   - Preserve admin return navigation and provide the authenticated customer link.
5. **Verification**
   - Regenerate API transports, shared types, schemas, and remote functions after contract changes.
   - Run focused tests while iterating, then relevant backend/frontend quality gates.

## Acceptance coverage

- Admin creation defaults to Confirmed/unpaid and belongs to the selected customer; Pending creation expires normally.
- Customer ownership makes the booking visible in the customer's existing booking screens.
- Facility access is enforced for all admin actions; customer endpoints retain owner-only protections where appropriate.
- Creation and new/changed contracts use the selected customer's eligibility; unchanged contracts can survive later eligibility changes.
- Daily schedule shows availability and provides create/manage entry points; list navigation remains available.
- Moves stay within the facility, show repricing, preserve status, and handle incompatible extras/contracts.
- Concurrent creation, moves, and restoration cannot exceed capacity.
- Cancellation retains player details and releases capacity; restoration fails if capacity has been taken.
- Pending restoration receives a fresh expiry window.
- Edits cannot reduce the total below paid amounts plus pending payment reservations.
- Confirmed unpaid bookings support checkout; cancelled/expired bookings do not.
- Admins can view payment history without spending customer vouchers or manually marking payment received.
- Payment return navigation retains admin context; copied customer links remain authenticated customer routes.
- Admin actions record actor and time, and existing customer booking flows remain functional.

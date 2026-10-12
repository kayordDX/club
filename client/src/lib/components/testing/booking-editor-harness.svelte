<script lang="ts">
	import BookingDetailsForm from "$lib/components/booking/BookingDetailsForm.svelte";
	import type { BookingFormSubmitHandler } from "$lib/booking/schema";
	import type { ResolvedPathname } from "$app/types";
	let { onSubmit, legacy = false, inactive = false }: { onSubmit: BookingFormSubmitHandler; legacy?: boolean; inactive?: boolean } = $props();
	let moved = $state(false);
	let loading = $state(false);
</script>

<button onclick={() => (moved = !moved)}>Change destination</button>
<button onclick={() => (loading = !loading)}>Toggle loading</button>
<BookingDetailsForm
	title="Edit booking details"
	description="Test editor"
	submitLabel="Save changes"
	isSubmitting={false}
	backHref={"/bookings" as ResolvedPathname}
	backLabel="Back"
	slotId={moved ? "destination" : "source"}
	facilityId={1}
	date={moved ? "2026-08-03" : "2026-08-02"}
	dateLabel={moved ? "Destination day" : "Source day"}
	initialPlayers={[
		{ name: "Player One", cellNo: "0821234567", email: "player@example.test", contractId: "31" },
		...(legacy ? [{ name: "Player Two", cellNo: "0821234567", email: "two@example.test", contractId: "32" }] : []),
	]}
	initialExtras={[{ id: 5, name: "Cart", amount: 1, price: 25 }]}
	availableContracts={moved
		? [{ id: 41, contractName: "Destination Visitor", price: 150 }]
		: legacy
			? [{ id: 33, contractName: "Public", price: 200 }]
			: [{ id: 31, contractName: "Visitor", price: 100 }]}
	preservedContracts={!moved && legacy
		? [
				{ id: 31, contractName: "Legacy One", price: 100 },
				{ id: 32, contractName: "Legacy Two", price: 100 },
			]
		: []}
	availableExtras={moved ? [] : [{ id: 5, name: "Cart", price: 30 }]}
	authorizedAvailability
	slotAvailability={{ total: 4, booked: inactive ? 4 : 1, startDatetime: "2026-08-02T10:00:00Z" }}
	enforceCapacity={!inactive}
	optionsLoading={loading}
	showProfileShortcut={false}
	{onSubmit}
/>

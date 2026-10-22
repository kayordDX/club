<script lang="ts">
	import { Button, Skeleton } from "@kayord/ui";
	import { BuildingIcon, UserCogIcon } from "@lucide/svelte";
	import { parseDate, today, getLocalTimeZone } from "@internationalized/date";
	import { page } from "$app/state";
	import { goto } from "$app/navigation";
	import type { ResolvedPathname } from "$app/types";
	import { resolve } from "$app/paths";
	import { createSearchParamsSchema, useSearchParams } from "runed/kit";
	import { slotAvailable, slotGetAll } from "$lib/api/remote/slot.remote";
	import { toast } from "svelte-sonner";
	import { adminSlotGetAll } from "$lib/api/remote/admin.remote";
	import { hasRoles, useUser } from "$lib/auth";
	import { openAdminBookingCreation } from "$lib/booking/admin-creation-navigation";
	import Slots from "$lib/components/schedule/slots.svelte";
	import ScheduleDateNavigation from "$lib/components/schedule/schedule-date-navigation.svelte";
	import PageBoundary from "$lib/components/PageBoundary.svelte";

	const searchParamsSchema = createSearchParamsSchema({
		date: { type: "string", default: today(getLocalTimeZone()).toString() },
	});

	const params = useSearchParams(searchParamsSchema, { noScroll: true });

	const value = $derived.by(() => {
		try {
			return parseDate(params.date);
		} catch {
			return today(getLocalTimeZone());
		}
	});

	// Facility managers see the same day's slots, but rendered as admin cards that
	// list who booked each slot (players + status) instead of the public pricing UI.
	const isAdmin = $derived(hasRoles("MANAGER"));
	const user = useUser();
	const slots = $derived(
		isAdmin
			? adminSlotGetAll({ facilityId: Number(page.params.id), params: { date: value.toDate("UTC").toISOString() } })
			: slotGetAll({ facilityId: Number(page.params.id), date: value.toDate("UTC").toISOString() })
	);

	const createBooking = async (slotId: string, slotCount: number, date: string) => {
		try {
			const isAvailable = await slotAvailable({ id: slotId, slotCount });
			if (!isAvailable) {
				toast.error("Not enough slots available");
				await slots.refresh();
				return;
			}
			const pathname = resolve("/(protected)/outlet/[slug]/[id]/slot/[slot]", {
				slug: page.params.slug ?? "",
				id: page.params.id ?? "",
				slot: slotId,
			});
			const href = `${pathname}?${new URLSearchParams({ slotCount: String(slotCount), date })}` as ResolvedPathname;
			if (user) await goto(href);
			else window.location.href = `/auth/login?next=${encodeURIComponent(href)}`;
		} catch {
			toast.error("Unable to verify slot availability. Please try again.");
		}
	};

	const managerCreateBooking = (slotId: string, date: string) =>
		openAdminBookingCreation({
			slug: page.params.slug ?? "",
			facilityId: page.params.id ?? "",
			slotId,
			date,
			onUnavailable: () => slots.refresh(),
		});

	const bookingHref = (bookingId: number) =>
		resolve("/(protected)/outlet/[slug]/[id]/admin/bookings/[bookingId]", {
			slug: page.params.slug ?? "",
			id: page.params.id ?? "",
			bookingId: String(bookingId),
		});
</script>

<div class="flex flex-wrap items-center justify-between gap-3 px-1 py-3">
	<ScheduleDateNavigation {value} onChange={(date) => (params.date = date)} />
	<div class="flex items-center gap-2">
		{#if hasRoles("MANAGER")}
			<Button variant="destructive" href={resolve(`/outlet/${page.params.slug}/${page.params.id}/admin`)}>
				<UserCogIcon />
				Admin
			</Button>
		{/if}
		<Button href={resolve(`/outlet/${page.params.slug}/${page.params.id}/facility`)}>
			<BuildingIcon />
			<span class="hidden sm:inline"> Facility </span>
		</Button>
	</div>
</div>
<div class="px-1">
	<PageBoundary>
		{#if slots.loading}
			<div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
				<Skeleton class="h-25 w-full" />
				<Skeleton class="h-25 w-full" />
				<Skeleton class="h-25 w-full" />
				<Skeleton class="h-25 w-full" />
				<Skeleton class="h-25 w-full" />
				<Skeleton class="h-25 w-full" />
			</div>
		{:else}
			<Slots
				slots={await slots}
				selectedDate={value.toString()}
				{isAdmin}
				adminContext={isAdmin}
				onCreateBooking={managerCreateBooking}
				onPublicBook={createBooking}
				{bookingHref}
			/>
		{/if}
	</PageBoundary>
</div>

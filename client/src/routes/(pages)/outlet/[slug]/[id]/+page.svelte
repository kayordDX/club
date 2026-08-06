<script lang="ts">
	import { Button, Popover, ButtonGroup, Loader } from "@kayord/ui";
	import { Calendar } from "@kayord/ui/calendar";
	import { CalendarIcon, ChevronRightIcon, ChevronLeftIcon, BuildingIcon, UserCogIcon } from "@lucide/svelte";
	import { parseDate, today, getLocalTimeZone, DateFormatter, type DateValue } from "@internationalized/date";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { cn } from "@kayord/ui/utils";
	import { createSearchParamsSchema, useSearchParams } from "runed/kit";
	import { slotGetAll } from "$lib/api/remote/slot.remote";
	import { useUser } from "$lib/auth";
	import Slots from "./Slots.svelte";
	import PageBoundary from "$lib/components/PageBoundary.svelte";

	const user = useUser();

	const df = new DateFormatter("en-ZA", {
		dateStyle: "long",
	});

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

	const incrementDate = (incrementValue: number) => {
		params.date = value.add({ days: incrementValue }).toString();
	};

	// Reactive remote query — re-fetches when the facility or selected date changes.
	const slots = $derived(
		slotGetAll({
			facilityId: Number(page.params.id),
			date: value.toDate(getLocalTimeZone()).toISOString(),
		})
	);
</script>

<div class="flex flex-row items-center gap-2">
	<div class="flex w-full flex-col gap-2">
		<div class="flex-row gap-4 py-4 sm:flex">
			<div class="hidden flex-col sm:flex">
				<div class="font-bold">Select Date</div>
				<div class="text-muted-foreground text-xs">Pick a day</div>
			</div>
			<Popover.Root>
				<Popover.Trigger>
					{#snippet child({ props })}
						<ButtonGroup.Root>
							<Button size="icon" variant="outline" onclick={() => incrementDate(-1)}>
								<ChevronLeftIcon />
							</Button>
							<Button variant="outline" class={cn("w-70 justify-start text-start font-normal", !value && "text-muted-foreground")} {...props}>
								<CalendarIcon class="me-2 size-4" />
								{value ? df.format(value.toDate(getLocalTimeZone())) : "Select a date"}
							</Button>
							<Button size="icon" variant="outline" onclick={() => incrementDate(1)}>
								<ChevronRightIcon />
							</Button>
						</ButtonGroup.Root>
					{/snippet}
				</Popover.Trigger>
				<Popover.Content class="w-auto p-0">
					<Calendar
						{value}
						onValueChange={(v: DateValue | undefined) => v && (params.date = v.toString())}
						type="single"
						initialFocus
						captionLayout="dropdown"
					/>
				</Popover.Content>
			</Popover.Root>
		</div>
	</div>
	<div class="flex items-center gap-2">
		{#if user}
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
<div>
	<PageBoundary>
		{#if slots.loading}
			<Loader />
		{:else}
			<Slots slots={await slots} selectedDate={value.toString()} refetch={() => slots.refresh()} />
		{/if}
	</PageBoundary>
</div>

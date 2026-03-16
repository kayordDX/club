<script lang="ts">
	import { Button, Breadcrumb, Popover, ButtonGroup } from "@kayord/ui";
	import { Calendar } from "@kayord/ui/calendar";
	import { CalendarIcon, ChevronRightIcon, ChevronLeftIcon, BuildingIcon } from "@lucide/svelte";
	import {
		parseDate,
		today,
		getLocalTimeZone,
		DateFormatter,
		type DateValue,
	} from "@internationalized/date";
	import { createOutletGet } from "$lib/api";
	import { page } from "$app/state";
	import Query from "$lib/components/Query.svelte";
	import { resolve } from "$app/paths";
	import { cn } from "@kayord/ui/utils";
	import { createSlotGetAll } from "$lib/api";
	import Slots from "./Slots.svelte";

	const query = createOutletGet(() => page.params.slug ?? "");
	const outlet = $derived(query.data);

	const facility = $derived(outlet?.facilities.find((x) => x.id == Number(page.params.id)));

	const df = new DateFormatter("en-ZA", {
		dateStyle: "long",
	});

	const initialDate = page.url.searchParams.get("date") ?? today(getLocalTimeZone()).toString();

	let value = $state<DateValue>(parseDate(initialDate));

	const incrementDate = (incrementValue: number) => {
		value = value.add({ days: incrementValue });
	};

	const slotsQuery = createSlotGetAll(() => ({
		facilityId: Number(page.params.id),
		date: value.toString(),
	}));
	const slotsData = $derived(slotsQuery.data ?? []);
</script>

<div class="m-4">
	<Query {query} emptyText="Unable to load outlet">
		<Breadcrumb.Root class="mb-4">
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/">Home</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Link href={resolve(`/outlet/${page.params.slug}/book`)}>
						{outlet!.name}
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page>{facility?.name}</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>

		<div class="flex flex-row gap-2">
			<div class="flex w-full flex-col gap-2">
				<div class="flex flex-row gap-4 p-4">
					<div>
						<div class="font-bold">Select Date</div>
						<div class="text-muted-foreground text-xs">Pick a day</div>
					</div>
					<Popover.Root>
						<Popover.Trigger>
							{#snippet child({ props })}
								<ButtonGroup.Root class="hidden sm:flex">
									<Button size="icon" variant="outline" onclick={() => incrementDate(-1)}>
										<ChevronLeftIcon />
									</Button>
									<Button
										variant="outline"
										class={cn(
											"w-70 justify-start text-start font-normal",
											!value && "text-muted-foreground"
										)}
										{...props}
									>
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
							<Calendar bind:value type="single" initialFocus captionLayout="dropdown" />
						</Popover.Content>
					</Popover.Root>
				</div>
			</div>
			<div>
				<Button href={resolve(`/outlet/${page.params.slug}/facility/${page.params.id}`)}>
					<BuildingIcon />
					Facility Information
				</Button>
			</div>
		</div>
		<div>
			<Slots slots={slotsData} selectedDate={value.toString()} />
		</div>
	</Query>
</div>

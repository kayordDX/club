<script lang="ts">
	import {
		Card,
		Button,
		Badge,
		Avatar,
		Separator,
		Breadcrumb,
		Popover,
		ButtonGroup,
	} from "@kayord/ui";
	import { Calendar } from "@kayord/ui/calendar";
	import { CalendarDays, CalendarIcon, ChevronRightIcon, ChevronLeftIcon } from "@lucide/svelte";
	import Times from "./Times.svelte";
	import { today, getLocalTimeZone, DateFormatter, type DateValue } from "@internationalized/date";
	import Rules from "./Rules.svelte";
	import { createOutletGet } from "$lib/api";
	import { page } from "$app/state";
	import Query from "$lib/components/Query.svelte";
	import { resolve } from "$app/paths";
	import { cn } from "@kayord/ui/utils";
	import { createSlotGetAll } from "$lib/api";
	import Slots from "./Slots.svelte";

	// Inlined club data
	const club = {
		name: "Green Valley Golf & Paddle Club",
		type: "Golf & Paddle",
		location: "123 Fairway Rd, Cityville",
		description:
			"A premier club offering world-class golf and paddle facilities. Book your slot and enjoy a great experience!",
		image: "/club-image.png",
		amenities: ["18-hole Golf Course", "4 Paddle Courts", "Clubhouse", "Pro Shop", "Restaurant"],
		contact: {
			phone: "(555) 123-4567",
			email: "info@greenvalleyclub.com",
		},
	};

	const query = createOutletGet(() => page.params.slug ?? "");
	const outlet = $derived(query.data);

	const facility = $derived(outlet?.facilities.find((x) => x.id == Number(page.params.id)));

	const df = new DateFormatter("en-ZA", {
		dateStyle: "long",
	});

	let value = $state<DateValue>(today(getLocalTimeZone()));

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

		<div class="flex flex-col gap-2 md:flex-row">
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
				<Slots slots={slotsData} />
			</div>
			<div class="flex flex-col gap-2">
				<Card.Root class="p-2">
					<Card.Root class="w-full">
						<Card.Header class="flex items-center gap-4">
							<Avatar.Root>
								<Avatar.Image src={club.image} alt={club.name} />
								<Avatar.Fallback>{club.name[0]}</Avatar.Fallback>
							</Avatar.Root>
							<div>
								<Card.Title class="flex items-center gap-2 text-2xl font-bold">
									{club.name}
									<Badge variant="secondary">{club.type}</Badge>
								</Card.Title>
								<Card.Description class="flex items-center gap-2 text-gray-500">
									<CalendarDays class="h-4 w-4" />
									{club.location}
								</Card.Description>
							</div>
						</Card.Header>
						<Card.Content>
							<p class="mb-2">{club.description}</p>
							<div class="mb-2 flex flex-wrap gap-2">
								{#each club.amenities as amenity (amenity)}
									<Badge>{amenity}</Badge>
								{/each}
							</div>
							<Separator />
							<div class="mt-2 text-sm text-gray-600">
								<span class="font-semibold">Contact:</span>
								{club.contact.phone} | {club.contact.email}
							</div>
						</Card.Content>
					</Card.Root>
					<Times />
					<Rules />
				</Card.Root>
			</div>
		</div>
	</Query>
</div>

<script lang="ts">
	import { Card, Button, Badge, Table, Avatar, Separator, Breadcrumb } from "@kayord/ui";
	import { Calendar } from "@kayord/ui/calendar";
	import { CalendarDays, Clock, CircleXIcon, CircleCheckIcon } from "@lucide/svelte";
	import Times from "./Times.svelte";
	import { today, getLocalTimeZone } from "@internationalized/date";
	import Facility from "./Facility.svelte";
	import Rules from "./Rules.svelte";
	import { createOutletGet } from "$lib/api";
	import { page } from "$app/state";
	import Query from "$lib/components/Query.svelte";
	import { resolve } from "$app/paths";

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

	// Inlined booking slots
	const slots = [
		{ id: 1, type: "Golf", time: "08:00 - 09:30", available: true },
		{ id: 2, type: "Golf", time: "10:00 - 11:30", available: false },
		{ id: 3, type: "Golf", time: "12:00 - 13:30", available: true },
		{ id: 4, type: "Paddle", time: "09:00 - 10:00", available: true },
		{ id: 5, type: "Paddle", time: "11:00 - 12:00", available: false },
		{ id: 6, type: "Paddle", time: "13:00 - 14:00", available: true },
	];

	let selectedDate = $state(today(getLocalTimeZone()).add({ days: 5 }));

	const query = createOutletGet(() => page.params.slug ?? "");
	const outlet = $derived(query.data);

	const facility = $derived(outlet?.facilities.find((x) => x.id == Number(page.params.id)));
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
				<Card.Root>
					<Card.Header>
						<Card.Title>Select Date</Card.Title>
						<Card.Description>Pick a day</Card.Description>
					</Card.Header>
					<Card.Content>
						<Calendar
							type="single"
							bind:value={selectedDate}
							class="w-fit rounded-md border shadow-sm"
						/>
					</Card.Content>
				</Card.Root>
				<Table.Root>
					<Table.Header>
						<Table.Row>
							<Table.Head>Type</Table.Head>
							<Table.Head>Time</Table.Head>
							<Table.Head>Status</Table.Head>
							<Table.Head></Table.Head>
						</Table.Row>
					</Table.Header>
					<Table.Body>
						{#each slots as slot (slot)}
							<Table.Row>
								<Table.Cell>{slot.type}</Table.Cell>
								<Table.Cell><Clock class="mr-1 inline h-4 w-4" /> {slot.time}</Table.Cell>
								<Table.Cell>
									{#if slot.available}
										<Badge><CircleCheckIcon class="inline h-4 w-4" /> Available</Badge>
									{:else}
										<Badge variant="destructive"
											><CircleXIcon class="inline h-4 w-4" /> Booked</Badge
										>
									{/if}
								</Table.Cell>
								<Table.Cell>
									<Button disabled={!slot.available} onclick={() => console.log(slot)}>Book</Button>
								</Table.Cell>
							</Table.Row>
						{/each}
					</Table.Body>
				</Table.Root>
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

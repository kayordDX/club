<script lang="ts">
	import {
		Card,
		Button,
		Badge,
		Table,
		Avatar,
		Alert,
		Tabs,
		Separator,
		Pagination,
		Breadcrumb,
	} from "@kayord/ui";
	import { Calendar } from "@kayord/ui/calendar";
	import { CalendarDays, User, Clock, CircleXIcon, CircleCheckIcon } from "@lucide/svelte";
	import { getAccountMeQueryOptions } from "$lib/api";
	import { createQuery } from "@tanstack/svelte-query";
	import Times from "./Times.svelte";
	import { today, getLocalTimeZone } from "@internationalized/date";
	import Facility from "./Facility.svelte";

	const query = createQuery(() => getAccountMeQueryOptions());

	console.log("User Data:", query.data);

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

	// Inlined bookings (for demo)
	const bookings = [
		{ id: 1, user: "Jane Doe", type: "Golf", time: "08:00 - 09:30" },
		{ id: 2, user: "John Smith", type: "Paddle", time: "09:00 - 10:00" },
	];

	let selectedTab = $state("slots");
	let selectedSlot = $state<Slot | undefined>(undefined);
	let showAlert = $state(false);

	type Slot = { id: number; type: string; time: string; available: boolean };
	function bookSlot(slot: Slot) {
		selectedSlot = slot;
		showAlert = true;
		setTimeout(() => {
			showAlert = false;
		}, 2000);
	}

	let selectedDate = $state(today(getLocalTimeZone()).add({ days: 5 }));
</script>

<Breadcrumb.Root>
	<Breadcrumb.List>
		<Breadcrumb.Item>
			<Breadcrumb.Link href="/">Home</Breadcrumb.Link>
		</Breadcrumb.Item>
		<Breadcrumb.Separator />
		<Breadcrumb.Item>
			<Breadcrumb.Page>Club Name</Breadcrumb.Page>
		</Breadcrumb.Item>
	</Breadcrumb.List>
</Breadcrumb.Root>
<div class="mx-auto flex max-w-3xl flex-col items-center gap-8 px-4 py-8">
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

	<Facility />
	<div class="flex items-center gap-2">
		<Times />
		<Calendar type="single" bind:value={selectedDate} class="rounded-md border shadow-sm" />
	</div>

	<Tabs.Root value={selectedTab} class="w-full">
		<Tabs.List>
			<Tabs.Trigger value="slots">Available Slots</Tabs.Trigger>
			<Tabs.Trigger value="bookings">My Bookings</Tabs.Trigger>
		</Tabs.List>
		<Tabs.Content value="slots">
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
									<Badge variant="destructive"><CircleXIcon class="inline h-4 w-4" /> Booked</Badge>
								{/if}
							</Table.Cell>
							<Table.Cell>
								<Button disabled={!slot.available} onclick={() => bookSlot(slot)}>Book</Button>
							</Table.Cell>
						</Table.Row>
					{/each}
				</Table.Body>
			</Table.Root>
			<Pagination.Root count={100} perPage={10}>
				{#snippet children({ pages, currentPage })}
					<Pagination.Content>
						<Pagination.Item>
							<Pagination.PrevButton />
						</Pagination.Item>
						{#each pages as page (page.key)}
							{#if page.type === "ellipsis"}
								<Pagination.Item>
									<Pagination.Ellipsis />
								</Pagination.Item>
							{:else}
								<Pagination.Item>
									<Pagination.Link {page} isActive={currentPage === page.value}>
										{page.value}
									</Pagination.Link>
								</Pagination.Item>
							{/if}
						{/each}
						<Pagination.Item>
							<Pagination.NextButton />
						</Pagination.Item>
					</Pagination.Content>
				{/snippet}
			</Pagination.Root>
			{#if showAlert}
				<Alert.Root class="mt-4">
					<Alert.Title>Success!</Alert.Title>
					<Alert.Description>Slot booked successfully!</Alert.Description>
				</Alert.Root>
			{/if}
		</Tabs.Content>
		<Tabs.Content value="bookings">
			<Table.Root>
				<Table.Header>
					<Table.Row>
						<Table.Head>User</Table.Head>
						<Table.Head>Type</Table.Head>
						<Table.Head>Time</Table.Head>
					</Table.Row>
				</Table.Header>
				<Table.Body>
					{#each bookings as booking (booking)}
						<Table.Row>
							<Table.Cell><User class="h-4 w-4" /> {booking.user}</Table.Cell>
							<Table.Cell>{booking.type}</Table.Cell>
							<Table.Cell>{booking.time}</Table.Cell>
						</Table.Row>
					{/each}
				</Table.Body>
			</Table.Root>
		</Tabs.Content>
	</Tabs.Root>
</div>

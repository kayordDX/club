<script lang="ts">
	import { Card, Badge, Avatar, Separator, Breadcrumb, Button } from "@kayord/ui";
	import { CalendarDays, TicketIcon } from "@lucide/svelte";
	import Times from "./Times.svelte";
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

		<div class="flex flex-col gap-2">
			<Card.Root>
				<Card.Header>
					<Card.Title class="text-3xl font-bold">Bookings</Card.Title>
					<Card.Description class="text-gray-500"
						>Book your slot at {facility?.name}
					</Card.Description>
				</Card.Header>
				<Card.Footer>
					<Button href={resolve(`/outlet/${page.params.slug}/book/${page.params.id}`)}>
						<TicketIcon />
						Book Now
					</Button>
				</Card.Footer>
			</Card.Root>
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
		</div>
	</Query>
</div>

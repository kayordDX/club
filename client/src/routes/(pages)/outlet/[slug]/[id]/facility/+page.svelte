<script lang="ts">
	import { Card, Badge, Avatar, Separator, Button, Item } from "@kayord/ui";
	import { BuildingIcon, CalendarDays, MailIcon, PhoneIcon, TicketIcon } from "@lucide/svelte";
	import { createFacilityGet, createOutletGet } from "$lib/api";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import Query from "$lib/components/Query.svelte";
	import { Markdown } from "$lib/components/Markdown";
	import { Tags } from "$lib/components/Tags";

	const query = createFacilityGet(
		() => Number(page.params.id),
		() => ({
			query: { staleTime: 1000 * 60 * 5 },
		})
	);

	const outletQuery = createOutletGet(
		() => page.params.slug ?? "",
		() => ({
			query: { staleTime: 1000 * 60 * 5 },
		})
	);
</script>

<Query {query} emptyText="Unable to load facility">
	<div class="flex flex-col gap-2 pt-4">
		<Query query={outletQuery} emptyText="Unable to load outlet">
			<Card.Root class="w-full">
				<Card.Header class="flex items-center gap-4">
					<Avatar.Root>
						<Avatar.Image src={outletQuery.data?.logo} alt={outletQuery.data?.name} />
						<Avatar.Fallback>{outletQuery.data?.name[0]}</Avatar.Fallback>
					</Avatar.Root>
					<div>
						<Card.Title class="flex items-center gap-2 text-2xl font-bold">
							{outletQuery.data?.name}
							<Badge variant="secondary">{outletQuery.data?.outletType.name}</Badge>
						</Card.Title>
						<Card.Description class="flex items-center gap-2 text-gray-500">
							<CalendarDays class="h-4 w-4" />
							{outletQuery.data?.address}
						</Card.Description>
					</div>
				</Card.Header>
				<Card.Content>
					<Markdown
						source={outletQuery.data?.description ?? ""}
						class="prose-p:text-muted-foreground mb-2 text-sm"
					/>
					<Tags tags={outletQuery.data?.tags ?? ""} />
					<Separator class="my-4" />
					<div class="text-muted-foreground text-sm">
						<span class="font-semibold">Contact:</span>
						{outletQuery.data?.contact} | {outletQuery.data?.email}
					</div>
				</Card.Content>
				<Card.Footer class="flex flex-col items-start gap-2">
					<Button href={resolve(`/outlet/${page.params.slug}/info`)} variant="outline">
						<BuildingIcon />
						Outlet
					</Button>
				</Card.Footer>
			</Card.Root>
		</Query>

		<Card.Root>
			<Card.Header>
				<Card.Title class="text-3xl font-bold">Bookings</Card.Title>
				<Card.Description class="text-gray-500">
					Book your slot at {query.data?.name}
				</Card.Description>
			</Card.Header>
			<Card.Footer>
				<Button href={resolve(`/outlet/${page.params.slug}/${page.params.id}`)}>
					<TicketIcon />
					Book Now
				</Button>
			</Card.Footer>
		</Card.Root>

		<Card.Root>
			<Card.Header>
				<Card.Title>Facility Contact Info</Card.Title>
			</Card.Header>
			<Card.Content>
				<Item.Group>
					<Item.Root variant="muted">
						<Item.Media>
							<Avatar.Root>
								<Avatar.Fallback><PhoneIcon class="text-primary size-4" /></Avatar.Fallback>
							</Avatar.Root>
						</Item.Media>
						<Item.Content class="gap-1">
							<Item.Title>Phone</Item.Title>
							<Item.Description>{query.data?.contact}</Item.Description>
						</Item.Content>
					</Item.Root>
					<Item.Root variant="muted">
						<Item.Media>
							<Avatar.Root>
								<Avatar.Fallback><MailIcon class="text-primary size-4" /></Avatar.Fallback>
							</Avatar.Root>
						</Item.Media>
						<Item.Content class="gap-1">
							<Item.Title>Email</Item.Title>
							<Item.Description>{query.data?.email}</Item.Description>
						</Item.Content>
					</Item.Root>
				</Item.Group>
			</Card.Content>
		</Card.Root>

		<Card.Root>
			<Card.Header>
				<Card.Title>Facility Operating Hours</Card.Title>
			</Card.Header>
			<Card.Content>
				<Markdown source={query.data?.operatingHours ?? ""} />
			</Card.Content>
		</Card.Root>

		<Card.Root>
			<Card.Header>
				<Card.Title>Rules</Card.Title>
			</Card.Header>
			<Card.Content>
				<Markdown source={query.data?.rules ?? ""} />
			</Card.Content>
		</Card.Root>
	</div>
</Query>

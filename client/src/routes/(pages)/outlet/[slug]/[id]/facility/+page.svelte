<script lang="ts">
	import { Card, Badge, Avatar, Separator, Button, Item } from "@kayord/ui";
	import { BuildingIcon, CalendarDays, MailIcon, PhoneIcon, TicketIcon } from "@lucide/svelte";
	import { facilityGet } from "$lib/api/remote/facility.remote";
	import { outletGet } from "$lib/api/remote/outlet.remote";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import Await from "$lib/components/Await.svelte";
	import { Markdown } from "$lib/components/Markdown";
	import { Tags } from "$lib/components/Tags";

	const facility = facilityGet(Number(page.params.id));
	const outlet = outletGet(page.params.slug ?? "");
</script>

<Await promise={facility} emptyText="Unable to load facility">
	{#snippet children(f)}
		<div class="flex flex-col gap-2 pt-4">
			<Await promise={outlet} emptyText="Unable to load outlet">
				{#snippet children(o)}
					<Card.Root class="w-full">
						<Card.Header class="flex items-center gap-4">
							<Avatar.Root>
								<Avatar.Image src={o.logo} alt={o.name} />
								<Avatar.Fallback>{o.name[0]}</Avatar.Fallback>
							</Avatar.Root>
							<div>
								<Card.Title class="flex items-center gap-2 text-2xl font-bold">
									{o.name}
									<Badge variant="secondary">{o.outletType.name}</Badge>
								</Card.Title>
								<Card.Description class="flex items-center gap-2 text-gray-500">
									<CalendarDays class="h-4 w-4" />
									{o.address}
								</Card.Description>
							</div>
						</Card.Header>
						<Card.Content>
							<Markdown source={o.description ?? ""} class="prose-p:text-muted-foreground mb-2 text-sm" />
							<Tags tags={o.tags ?? ""} />
							<Separator class="my-4" />
							<div class="text-muted-foreground text-sm">
								<span class="font-semibold">Contact:</span>
								{o.contact} | {o.email}
							</div>
						</Card.Content>
						<Card.Footer class="flex flex-col items-start gap-2">
							<Button href={resolve(`/outlet/${page.params.slug}/info`)} variant="outline">
								<BuildingIcon />
								Outlet
							</Button>
						</Card.Footer>
					</Card.Root>
				{/snippet}
			</Await>

			<Card.Root>
				<Card.Header>
					<Card.Title class="text-3xl font-bold">Bookings</Card.Title>
					<Card.Description class="text-gray-500">
						Book your slot at {f.name}
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
								<Item.Description>{f.contact}</Item.Description>
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
								<Item.Description>{f.email}</Item.Description>
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
					<Markdown source={f.operatingHours ?? ""} />
				</Card.Content>
			</Card.Root>

			<Card.Root>
				<Card.Header>
					<Card.Title>Rules</Card.Title>
				</Card.Header>
				<Card.Content>
					<Markdown source={f.rules ?? ""} />
				</Card.Content>
			</Card.Root>
		</div>
	{/snippet}
</Await>

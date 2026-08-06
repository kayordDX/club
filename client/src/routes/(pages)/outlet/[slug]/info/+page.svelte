<script lang="ts">
	import { Avatar, Button, Card, Item } from "@kayord/ui";
	import Breadcrumbs from "../Breadcrumbs.svelte";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { Building2Icon, BuildingIcon, MailIcon, MapPinIcon, PhoneIcon } from "@lucide/svelte";
	import { outletGet } from "$lib/api/remote/outlet.remote";
	import Await from "$lib/components/Await.svelte";
	import { Markdown } from "$lib/components/Markdown";
	import { Tags } from "$lib/components/Tags";

	const outlet = outletGet(page.params.slug ?? "");
</script>

<div class="m-2">
	<Breadcrumbs />

	<Await promise={outlet} emptyText="Unable to load outlet">
		{#snippet children(o)}
			<div class="mb-6 flex items-center justify-between gap-4">
				<div class="min-w-0 flex-1">
					<h1 class="text-3xl">{o.name}</h1>
					<h3 class="text-muted-foreground mb-1 flex items-center gap-2 text-sm">
						<BuildingIcon class="size-4" />
						{o.business.name}
					</h3>
					<Tags tags={o.tags ?? ""} />
				</div>
				<div class="shrink-0">
					<Button href={resolve(`/outlet/${page.params.slug}`)} variant="outline">
						<Building2Icon /> Choose Facility
					</Button>
				</div>
			</div>

			<div class="flex flex-col gap-4">
				<Card.Root>
					<Card.Header>
						<Card.Title>About</Card.Title>
					</Card.Header>
					<Card.Content>
						<Markdown source={o.description ?? ""} class="prose-p:text-muted-foreground text-sm" />
					</Card.Content>
				</Card.Root>

				<Card.Root>
					<Card.Header>
						<Card.Title>Contact Info</Card.Title>
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
									<Item.Description>{o.contact}</Item.Description>
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
									<Item.Description>{o.email}</Item.Description>
								</Item.Content>
							</Item.Root>
							<Item.Root variant="muted">
								<Item.Media>
									<Avatar.Root>
										<Avatar.Fallback><MapPinIcon class="text-primary size-4" /></Avatar.Fallback>
									</Avatar.Root>
								</Item.Media>
								<Item.Content class="gap-1">
									<Item.Title>Address</Item.Title>
									<Item.Description>{o.address}</Item.Description>
								</Item.Content>
							</Item.Root>
						</Item.Group>
					</Card.Content>
				</Card.Root>

				<Card.Root>
					<Card.Header>
						<Card.Title>Operating Hours</Card.Title>
					</Card.Header>
					<Card.Content>
						<Markdown source={o.operatingHours ?? ""} />
					</Card.Content>
				</Card.Root>
			</div>
		{/snippet}
	</Await>
</div>

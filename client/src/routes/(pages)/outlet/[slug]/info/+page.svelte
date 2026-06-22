<script lang="ts">
	import { Avatar, Button, Card, Item } from "@kayord/ui";
	import Breadcrumbs from "../Breadcrumbs.svelte";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { Building2Icon, BuildingIcon, MailIcon, MapPinIcon, PhoneIcon } from "@lucide/svelte";
	import { createOutletGet } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import { Markdown } from "$lib/components/Markdown";
	import { Tags } from "$lib/components/Tags";

	const query = createOutletGet(
		() => page.params.slug ?? "",
		() => ({
			query: { staleTime: 1000 * 60 * 5 },
		})
	);
</script>

<div class="m-2">
	<Breadcrumbs />

	<Query {query} emptyText="Unable to load outlet">
		<div class="mb-6 flex items-center justify-between gap-4">
			<div class="min-w-0 flex-1">
				<h1 class="text-3xl">{query.data?.name}</h1>
				<h3 class="text-muted-foreground mb-1 flex items-center gap-2 text-sm">
					<BuildingIcon class="size-4" />
					{query.data?.business.name}
				</h3>
				<Tags tags={query.data?.tags ?? ""} />
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
					<Markdown
						source={query.data?.description ?? ""}
						class="prose-p:text-muted-foreground text-sm"
					/>
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
						<Item.Root variant="muted">
							<Item.Media>
								<Avatar.Root>
									<Avatar.Fallback><MapPinIcon class="text-primary size-4" /></Avatar.Fallback>
								</Avatar.Root>
							</Item.Media>
							<Item.Content class="gap-1">
								<Item.Title>Address</Item.Title>
								<Item.Description>{query.data?.address}</Item.Description>
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
					<Markdown source={query.data?.operatingHours ?? ""} />
				</Card.Content>
			</Card.Root>
		</div>
	</Query>
</div>

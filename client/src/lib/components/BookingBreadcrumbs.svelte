<script lang="ts">
	import { resolve } from "$app/paths";
	import { createBookingGetPath } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import { Breadcrumb } from "@kayord/ui";
	import { HouseIcon } from "@lucide/svelte";
	import type { Snippet } from "svelte";

	type Props = {
		bookingId: number;
		children?: Snippet;
	};

	let { bookingId, children }: Props = $props();

	const query = createBookingGetPath(
		() => bookingId,
		() => ({ query: { enabled: bookingId > 0 } })
	);
	const path = $derived(query.data);

	const facilityHref = $derived.by(() => {
		if (!path) return "/";
		const date = path.slotStartDatetime?.slice(0, 10);
		return date ? `${resolve(`/outlet/${path.outletSlug}/${path.facilityId}`)}?date=${date}` : resolve(`/outlet/${path.outletSlug}/${path.facilityId}`);
	});
</script>

<div class="m-2">
	<Query {query} emptyText="Unable to load booking path">
		<Breadcrumb.Root>
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/">
						<HouseIcon class="size-3" />
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Link href={resolve(`/outlet/${path!.outletSlug}`)} class="text-xs">
						{path!.outletName}
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Link href={facilityHref} class="text-xs">
						{path!.facilityName}
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page class="text-xs">Edit Booking</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>
	</Query>
	{@render children?.()}
</div>

<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import { outletGetBasic } from "$lib/api/remote/outlet.remote";
	import { Breadcrumb, Loader, Skeleton } from "@kayord/ui";
	import { HouseIcon } from "@lucide/svelte";

	let { children } = $props();

	const outlet = $derived(await outletGetBasic(page.params.slug ?? ""));
	const facilityName = (o: { facilities: { id: number; name?: string | null }[] }) => o.facilities.find((x) => x.id == Number(page.params.id))?.name ?? "—";

	const facilityHref = $derived.by(() => {
		const date = page.url.searchParams.get("date");
		return date ? `${resolve(`/outlet/${page.params.slug}/${page.params.id}`)}?date=${date}` : resolve(`/outlet/${page.params.slug}/${page.params.id}`);
	});
</script>

<svelte:boundary>
	{#snippet pending()}
		<div class="m-2">
			<Skeleton class="h-4 w-95" />
		</div>
	{/snippet}
	<div class="m-2">
		<Breadcrumb.Root>
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/">
						<HouseIcon class="size-3" />
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Link href={resolve(`/outlet/${page.params.slug}`)} class="text-xs">
						{outlet.name}
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					{#if page.route.id?.includes("/slot/") || page.route.id?.includes("/booking/")}
						<Breadcrumb.Link href={facilityHref} class="text-xs">
							{facilityName(outlet)}
						</Breadcrumb.Link>
					{:else}
						<Breadcrumb.Page class="text-xs">
							{facilityName(outlet)}
						</Breadcrumb.Page>
					{/if}
				</Breadcrumb.Item>
				{#if page.route.id?.includes("/slot/")}
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						<Breadcrumb.Page class="text-xs">Player Details</Breadcrumb.Page>
					</Breadcrumb.Item>
				{/if}
				{#if page.route.id?.includes("/booking/")}
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						{#if page.params.bookingId}
							<Breadcrumb.Link href={resolve(`/bookings/${page.params.bookingId}/edit`)} class="text-xs">Player Details</Breadcrumb.Link>
						{:else if page.url.searchParams.get("slotId")}
							<Breadcrumb.Link
								href={resolve(`/outlet/${page.params.slug}/${page.params.id}/slot/${page.url.searchParams.get("slotId")}?${page.url.searchParams.toString()}`)}
								class="text-xs"
							>
								Player Details
							</Breadcrumb.Link>
						{:else}
							<Breadcrumb.Page class="text-xs">Player Details</Breadcrumb.Page>
						{/if}
					</Breadcrumb.Item>
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						<Breadcrumb.Page class="text-xs">Payment</Breadcrumb.Page>
					</Breadcrumb.Item>
				{/if}
			</Breadcrumb.List>
		</Breadcrumb.Root>
	</div>
</svelte:boundary>
{@render children?.()}

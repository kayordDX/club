<script lang="ts">
	import { page } from "$app/state";
	import { outletGetBasic } from "$lib/api/remote/outlet.remote";
	import { Breadcrumb } from "@kayord/ui";
	import Await from "$lib/components/Await.svelte";
	import { HouseIcon } from "@lucide/svelte";

	const outlet = outletGetBasic(page.params.slug ?? "");
</script>

<Await promise={outlet} emptyText="Unable to load outlet">
	{#snippet children(o)}
		<Breadcrumb.Root class="mb-4">
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/">
						<HouseIcon class="size-3" />
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page class="text-xs">{o.name}</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>
	{/snippet}
</Await>

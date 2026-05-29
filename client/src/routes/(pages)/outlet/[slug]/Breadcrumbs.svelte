<script lang="ts">
	import { page } from "$app/state";
	import { createOutletGetBasic } from "$lib/api";
	import { Breadcrumb } from "@kayord/ui";
	import Query from "$lib/components/Query.svelte";
	import { HouseIcon } from "@lucide/svelte";

	const query = createOutletGetBasic(
		() => page.params.slug ?? "",
		() => ({ query: { staleTime: 1000 * 60 * 5 } })
	);
	const outlet = $derived(query.data);
</script>

<Query {query} emptyText="Unable to load outlet">
	<Breadcrumb.Root class="mb-4">
		<Breadcrumb.List>
			<Breadcrumb.Item>
				<Breadcrumb.Link href="/">
					<HouseIcon class="size-3" />
				</Breadcrumb.Link>
			</Breadcrumb.Item>
			<Breadcrumb.Separator />
			<Breadcrumb.Item>
				<Breadcrumb.Page class="text-xs">{outlet!.name}</Breadcrumb.Page>
			</Breadcrumb.Item>
		</Breadcrumb.List>
	</Breadcrumb.Root>
</Query>

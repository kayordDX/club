<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import { createOutletGet } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import { Breadcrumb } from "@kayord/ui";
	let { children } = $props();

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
	</Query>
	{@render children?.()}
</div>

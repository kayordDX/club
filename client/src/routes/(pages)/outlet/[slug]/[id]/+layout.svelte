<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import { createOutletGetBasic } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import { Breadcrumb } from "@kayord/ui";
	import { HouseIcon } from "@lucide/svelte";
	import { auth } from "$lib/stores/auth.svelte";
	import { onMount } from "svelte";
	let { children } = $props();

	const query = createOutletGetBasic(
		() => page.params.slug ?? "",
		() => ({ query: { staleTime: 1000 * 60 * 5 } })
	);
	const outlet = $derived(query.data);

	onMount(async () => {
		if (auth.isAuthenticated) {
			await auth.getRoles(Number(page.params.id));
		}
	});

	const facility = $derived(outlet?.facilities.find((x) => x.id == Number(page.params.id)));
</script>

<div class="m-2">
	<Query {query} emptyText="Unable to load outlet">
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
						{outlet!.name}
					</Breadcrumb.Link>
				</Breadcrumb.Item>
				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page class="text-xs">
						{facility?.name}
					</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>
	</Query>
	{@render children?.()}
</div>

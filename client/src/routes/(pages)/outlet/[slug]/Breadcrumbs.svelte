<script lang="ts">
	import { page } from "$app/state";
	import { outletGetBasic } from "$lib/api/remote/outlet.remote";
	import { Breadcrumb } from "@kayord/ui";
	import { HouseIcon } from "@lucide/svelte";
</script>

<svelte:boundary>
	{@const outlet = await outletGetBasic(page.params.slug ?? "")}

	<Breadcrumb.Root class="mb-4">
		<Breadcrumb.List>
			<Breadcrumb.Item>
				<Breadcrumb.Link href="/">
					<HouseIcon class="size-3" />
				</Breadcrumb.Link>
			</Breadcrumb.Item>
			<Breadcrumb.Separator />
			<Breadcrumb.Item>
				<Breadcrumb.Page class="text-xs">{outlet.name}</Breadcrumb.Page>
			</Breadcrumb.Item>
		</Breadcrumb.List>
	</Breadcrumb.Root>
</svelte:boundary>

<script lang="ts">
	import { Button } from "@kayord/ui";
	import Breadcrumbs from "../Breadcrumbs.svelte";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { Building2Icon } from "@lucide/svelte";
</script>

<div class="m-2">
	<Breadcrumbs />

	<div class="flex items-center justify-between">
		<div>
			<h1 class="text-3xl">Outlet Information</h1>
			<h3 class="text-muted-foreground mb-6">Lets go</h3>
		</div>
		<div>
			<Button href={resolve(`/outlet/${page.params.slug}/book`)} variant="outline">
				<Building2Icon /> Choose Facility
			</Button>
		</div>
	</div>
</div>

<script lang="ts">
	import OutletBreadcrumbs from "$lib/components/OutletBreadcrumbs.svelte";
	import { setRolesContext } from "$lib/auth";

	let { data, children } = $props();
	// svelte-ignore state_referenced_locally
	setRolesContext(data.roles);
</script>

<OutletBreadcrumbs>
	{@render children?.()}
</OutletBreadcrumbs>

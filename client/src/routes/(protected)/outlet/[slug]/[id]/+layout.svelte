<script lang="ts">
	import OutletBreadcrumbs from "$lib/components/OutletBreadcrumbs.svelte";

	let { children } = $props();
</script>

<OutletBreadcrumbs>
	{@render children?.()}
</OutletBreadcrumbs>

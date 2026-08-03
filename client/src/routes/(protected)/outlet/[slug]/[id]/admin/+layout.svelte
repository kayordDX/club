<script lang="ts">
	import RoleCheck from "$lib/components/check/RoleCheck.svelte";

	let { children } = $props();
</script>

<RoleCheck roles={["MANAGER"]}>
	{@render children?.()}
</RoleCheck>

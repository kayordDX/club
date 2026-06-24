<script lang="ts">
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";
	import RoleCheck from "$lib/components/check/RoleCheck.svelte";

	let { children } = $props();
</script>

<AuthCheck isProtected={true}>
	<RoleCheck roles={["MANAGER"]}>
		{@render children?.()}
	</RoleCheck>
</AuthCheck>

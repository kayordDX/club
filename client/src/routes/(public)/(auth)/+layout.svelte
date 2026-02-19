<script lang="ts">
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";
	let { children } = $props();
</script>

<AuthCheck>
	{@render children?.()}
</AuthCheck>

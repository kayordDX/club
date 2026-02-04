<script lang="ts">
	import Header from "$lib/components/Header/Header.svelte";
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";
	let { children } = $props();
</script>

<AuthCheck>
	<Header />
	{@render children?.()}
</AuthCheck>

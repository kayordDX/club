<script lang="ts">
	import Header from "$lib/components/Header/Header.svelte";
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";
	import PageBoundary from "$lib/components/PageBoundary.svelte";
	let { children } = $props();
</script>

<AuthCheck>
	<Header />
	<PageBoundary>
		{@render children?.()}
	</PageBoundary>
</AuthCheck>

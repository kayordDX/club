<script lang="ts">
	import { createTest } from "$lib/api";

	const query = createTest({ name: "test" });
</script>

<div>Test Client</div>
{#if query.isLoading}
	<p>Loading...</p>
{:else}
	<p>Result: {JSON.stringify(query.data)}</p>
{/if}

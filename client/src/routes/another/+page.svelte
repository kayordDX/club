<script lang="ts">
	import { testOptions } from "$lib/api";
	import { createQuery } from "@tanstack/svelte-query";
	const testQuery = createQuery(() => testOptions({ query: { name: "doink" } }));
	const tests = $derived(testQuery.data);
</script>

<div>Another</div>
<pre>
	{JSON.stringify(tests, null, 2)}
</pre>

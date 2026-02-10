<script lang="ts">
	import { createTest } from "$lib/api";

	const testQuery = createTest(() => ({ name: "doink" }));
	const tests = $derived(testQuery.data);
</script>

<div>Another</div>
<pre>
	{JSON.stringify(tests, null, 2)}
</pre>

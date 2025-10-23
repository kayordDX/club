<script lang="ts">
	import { Button } from "@kayord/ui";
	import { test } from "./test.remote";
</script>

<div>Test Server</div>
{#if test().loading}
	<p>Loading...</p>
{:else}
	<p>Result: {JSON.stringify(test().current)}</p>
{/if}
<Button onclick={() => test().refresh()}>Reload</Button>
{test().ready}
<pre>{JSON.stringify(test().current)}</pre>

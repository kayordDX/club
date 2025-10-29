<script lang="ts">
	import { user } from "$lib/stores/user.svelte";
</script>

<div>{JSON.stringify(user.value)}</div>
{user.isLoading}

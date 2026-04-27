<script lang="ts">
	import { Button } from "@kayord/ui";
	import { auth } from "$lib/stores/auth.svelte";
</script>

{#if !auth.isAuthenticated}
	<Button onclick={auth.login} disabled={auth.isLoading}>Login</Button>
{/if}

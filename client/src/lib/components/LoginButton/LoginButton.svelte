<script lang="ts">
	import { Button } from "@kayord/ui";
	import { PUBLIC_API_URL } from "$env/static/public";
	import { user } from "$lib/stores/user.svelte";

	const handleLoginWithGoogle = () => {
		isLoading = true;
		window.location.href = `${PUBLIC_API_URL}/account/login/google?returnUrl=http://localhost:5173`;
	};

	let isLoading = $state(false);
</script>

{#if !user.isLoggedIn}
	<Button onclick={handleLoginWithGoogle} disabled={isLoading}>Login</Button>
{/if}

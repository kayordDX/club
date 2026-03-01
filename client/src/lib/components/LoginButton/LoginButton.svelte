<script lang="ts">
	import { Button } from "@kayord/ui";
	import { PUBLIC_API_URL, PUBLIC_APP_URL } from "$env/static/public";
	import { user } from "$lib/stores/user.svelte";
	import { page } from "$app/state";

	const redirect = $derived(`${PUBLIC_APP_URL}${page.url.searchParams.get("redirect")}`);

	const handleLoginWithGoogle = () => {
		isLoading = true;
		window.location.href = `${PUBLIC_API_URL}/account/login/google?returnUrl=${redirect}`;
	};

	let isLoading = $state(false);
</script>

{#if !user.isLoggedIn}
	<Button onclick={handleLoginWithGoogle} disabled={isLoading}>Login</Button>
{/if}

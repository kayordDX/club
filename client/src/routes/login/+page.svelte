<script lang="ts">
	import { Button, Loader } from "@kayord/ui";
	import { PUBLIC_API_URL } from "$env/static/public";
	import GoogleSvg from "$lib/svg/GoogleSVG.svelte";
	import { user } from "$lib/stores/user.svelte";

	const handleLoginWithGoogle = () => {
		isLoading = true;
		window.location.href = `${PUBLIC_API_URL}/account/login/google?returnUrl=http://localhost:5173`;
	};

	let isLoading = $state(false);
</script>

<div class="m-4">
	{#if user.isLoggedIn}
		Logged in as {user.value?.firstName}
	{:else}
		<Button onclick={handleLoginWithGoogle} variant="outline">
			{#if isLoading}
				<Loader class="mr-2" />
			{:else}
				<GoogleSvg class="fill-white" />
			{/if}
			Google
		</Button>
	{/if}
</div>

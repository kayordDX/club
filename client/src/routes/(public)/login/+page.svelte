<script lang="ts">
	import { Button, Card, Loader, Separator } from "@kayord/ui";
	import { PUBLIC_API_URL } from "$env/static/public";
	import GoogleSvg from "$lib/svg/GoogleSVG.svelte";
	import { user } from "$lib/stores/user.svelte";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import LogoutButton from "$lib/components/LogoutButton/LogoutButton.svelte";

	const handleLoginWithGoogle = () => {
		isLoading = true;
		window.location.href = `${PUBLIC_API_URL}/account/login/google?returnUrl=http://localhost:5173`;
	};

	let isLoading = $state(false);
</script>

<div class="flex h-screen w-full flex-col items-center">
	<div class="flex h-full max-w-2xl flex-col items-center justify-center gap-6">
		<LogoButton />
		<Card.Root>
			<Card.Header>
				<Card.Title class="text-center">Welcome back</Card.Title>
				<Card.Description class="text-center">
					{user.isLoggedIn ? "You are already logged in" : "Sign in to book your next game"}
				</Card.Description>
			</Card.Header>
			<Card.Content class="flex flex-col items-center">
				{#if user.isLoggedIn}
					Logged in as {user.value?.firstName}
					<div class="mt-4">
						<LogoutButton />
					</div>
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
			</Card.Content>
			<Card.Footer class="flex flex-col items-center gap-2">
				<Separator />
				<p class="text-muted-foreground text-xs">
					We use Google to keep your account secure. No password needed.
				</p>
			</Card.Footer>
		</Card.Root>
		<p class="text-muted-foreground text-xs">
			By signing in, you agree to our Terms of Service and Privacy Policy
		</p>
	</div>
</div>

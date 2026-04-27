<script lang="ts">
	import { Button, Card, Loader } from "@kayord/ui";
	import { PUBLIC_APP_URL } from "$env/static/public";
	import { auth } from "$lib/stores/auth.svelte";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import LogoutButton from "$lib/components/LogoutButton/LogoutButton.svelte";
	import GoogleIcon from "$lib/svg/icons/GoogleIcon.svelte";
	import { page } from "$app/state";
	import LoginButton from "$lib/components/LoginButton/LoginButton.svelte";

	// const redirect = $derived(`${PUBLIC_APP_URL}${page.url.searchParams.get("redirect") ?? ""}`);
</script>

<div class="flex h-screen w-full flex-col items-center">
	<div class="flex h-full max-w-2xl flex-col items-center justify-center gap-6 p-2">
		<LogoButton />
		<Card.Root>
			<Card.Header>
				<Card.Title class="text-center">Welcome back</Card.Title>
				<Card.Description class="text-center">
					{auth.isAuthenticated ? "You are already logged in" : "Sign in to book your next game"}
				</Card.Description>
			</Card.Header>
			<Card.Content class="flex flex-col items-center">
				<LoginButton />
				<LogoutButton />
			</Card.Content>
			<Card.Footer class="flex flex-col items-center gap-2">
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

<script lang="ts">
	import { Card } from "@kayord/ui";
	import { page } from "$app/state";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import LogoutButton from "$lib/components/LogoutButton/LogoutButton.svelte";
	import LoginButton from "$lib/components/LoginButton/LoginButton.svelte";
	import { useUser } from "$lib/auth";

	const user = useUser();
	const redirect = page.url.searchParams.get("redirect") ?? undefined;
</script>

<div class="flex h-screen w-full flex-col items-center">
	<div class="flex h-full max-w-2xl flex-col items-center justify-center gap-6 p-2">
		<LogoButton />
		<Card.Root>
			<Card.Header>
				<Card.Title class="text-center">Welcome back</Card.Title>
				<Card.Description class="text-center">
					{user ? "You are already logged in" : "Sign in to book your next game"}
				</Card.Description>
			</Card.Header>
			<Card.Content class="flex flex-col items-center">
				<LoginButton returnUrl={redirect} />
				<LogoutButton />
			</Card.Content>
			<Card.Footer class="flex flex-col items-center gap-2">
				<p class="text-muted-foreground text-xs">We use Google to keep your account secure. No password needed.</p>
			</Card.Footer>
		</Card.Root>
		<p class="text-muted-foreground text-xs">By signing in, you agree to our Terms of Service and Privacy Policy</p>
	</div>
</div>

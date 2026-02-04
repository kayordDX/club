<script>
	import { Button } from "@kayord/ui";
	import Clubs from "./Clubs.svelte";
	import Search from "./Search.svelte";
	import { user } from "$lib/stores/user.svelte";
</script>

<main class="container mx-auto px-4 py-8">
	<div class="mb-12 text-center">
		<h1 class="text-foreground mb-4 text-4xl font-bold text-balance">Book Your Perfect Game</h1>
		<p class="text-muted-foreground mx-auto max-w-2xl text-xl text-pretty">
			Reserve paddle courts and golf slots at premium clubs. Select your preferred club to view
			available times and make your booking.
		</p>
		{#if !user.isLoggedIn}
			<Button class="mt-4" href="/login">Login</Button>
		{/if}
	</div>
	<Search />
	<Clubs />
</main>

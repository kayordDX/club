<script lang="ts">
	import { Button } from "@kayord/ui";
	import { LogOutIcon } from "@lucide/svelte";
	import { useUser } from "$lib/auth";

	const user = useUser();
</script>

{#if user}
	<Button href="/auth/logout" variant="destructive">
		<LogOutIcon />
		Logout
	</Button>
{/if}

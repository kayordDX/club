<script lang="ts">
	import { createForm } from "@tanstack/svelte-form";

	const form = createForm(() => ({
		defaultValues: { players: [{ name: "First player" }], revision: 0 },
	}));
	const players = form.useStore((state) => state.values.players);
</script>

<p>Same identity: {players.current === form.state.values.players}</p>
{#each players.current as player (player.name)}
	<p>{player.name}</p>
{/each}
<button onclick={() => form.setFieldValue("revision", (value) => value + 1)}>Update unrelated field</button>
<button onclick={() => form.setFieldValue("players", [{ name: "Replacement player" }])}>Replace players</button>

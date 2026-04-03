<script lang="ts">
	import { Field } from "@kayord/ui";
	import type { AnyFieldApi } from "@tanstack/svelte-form";

	let { field }: { field: AnyFieldApi } = $props();
</script>

<Field.Error>
	{#if field.state.meta.isTouched}
		{#each field.state.meta.errors as error (error)}
			<em>{error.message}</em>
		{/each}
		{field.state.meta.isValidating ? "Validating..." : ""}
	{/if}
</Field.Error>

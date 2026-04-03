<script lang="ts">
	import { Button } from "@kayord/ui";
	import { useFormContext } from "./index";
	import type { ComponentProps } from "svelte";

	const form = useFormContext();

	type Props = {} & ComponentProps<typeof Button>;

	const { ...props }: Props = $props();
</script>

<form.Subscribe selector={(state) => state.isSubmitting}>
	{#snippet children(state)}
		<Button type="submit" {...props} disabled={state} />
	{/snippet}
</form.Subscribe>

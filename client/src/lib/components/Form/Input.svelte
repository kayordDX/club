<script lang="ts">
	import { Field, Input } from "@kayord/ui";
	import { FieldError, isInvalid, useFieldContext } from "./index";
	import type { ComponentProps } from "svelte";

	type Props = { label?: string } & ComponentProps<typeof Input>;

	const field = useFieldContext<string>();

	const { label, ...props }: Props = $props();
</script>

<Field.Field data-invalid={isInvalid(field)}>
	{#if label}
		<Field.Label for={field.name}>{label}</Field.Label>
	{/if}
	<Input
		id={field.name}
		value={field.state.value}
		aria-invalid={isInvalid(field)}
		onblur={() => field.handleBlur()}
		oninput={(e) => field.handleChange((e.target as HTMLInputElement).value)}
		{...props}
	/>
	<FieldError {field} />
</Field.Field>

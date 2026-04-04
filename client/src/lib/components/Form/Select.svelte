<script lang="ts" generics="T extends string | number">
	import { Field, Select } from "@kayord/ui";
	import { FieldError, isInvalid, useFieldContext } from "./index";
	import type { ComponentProps } from "svelte";

	type SelectRootProps = ComponentProps<typeof Select.Root>;
	type SingleSelectRootProps = Extract<SelectRootProps, { type: "single" }>;
	type SelectItem<TValue extends string | number> = { value: TValue; label: string };

	type Props = { label?: string; items: SelectItem<T>[] } & Omit<SingleSelectRootProps, "type">;

	const field = useFieldContext<T>();

	const { label, items, ...props }: Props = $props();

	const stringValue = $derived(field.state.value == null ? "" : String(field.state.value));

	const triggerContent = $derived(
		items.find((item) => item.value === field.state.value)?.label ?? label ?? "Select an option"
	);

	const handleValueChange = (value: string) => {
		const selectedItem = items.find((item) => String(item.value) === value);

		if (selectedItem) {
			field.handleChange(selectedItem.value);
		}
	};
</script>

<Field.Field data-invalid={isInvalid(field)}>
	{#if label}
		<Field.Label for={field.name}>{label}</Field.Label>
	{/if}
	<Select.Root
		type="single"
		name={field.name}
		value={stringValue}
		onOpenChange={() => field.handleBlur()}
		onValueChange={handleValueChange}
		{...props}
	>
		<Select.Trigger aria-invalid={isInvalid(field)}>
			{triggerContent}
		</Select.Trigger>
		<Select.Content>
			<Select.Group>
				{#each items as item (String(item.value))}
					<Select.Item value={String(item.value)} label={item.label}>
						{item.label}
					</Select.Item>
				{/each}
			</Select.Group>
		</Select.Content>
	</Select.Root>
	<FieldError {field} />
</Field.Field>

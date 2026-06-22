<script lang="ts">
	import { isInvalid } from "$lib/components/Form";
	import { Button, ColorPicker, Field, Popover } from "@kayord/ui";
	import { type AnyFieldApi } from "@tanstack/svelte-form";
	type Props = {
		field: AnyFieldApi;
		label: string;
	};

	let { field, label }: Props = $props();
</script>

<Field.Field data-invalid={isInvalid(field)}>
	<Field.Label for={field.name} aria-invalid={isInvalid(field)}>{label}</Field.Label>
	<Popover.Root>
		<Popover.Trigger class="flex-1">
			{#snippet child({ props })}
				<div class="flex gap-2">
					<Button {...props} variant="outline" class="flex w-full">
						<div
							class="size-5 rounded-full border shadow-sm"
							style={`background-color: ${field.state.value};`}
						></div>
						{field.state.value}
					</Button>
				</div>
			{/snippet}
		</Popover.Trigger>
		<Popover.Content class="w-auto p-0">
			<div class="p-3">
				<ColorPicker
					formats={["hex"]}
					bind:value={
						() => (field.state.value as string) ?? "",
						(nextValue: string) => field.handleChange(nextValue)
					}
				/>
			</div>
		</Popover.Content>
	</Popover.Root>
	<Field.Description>This appears on invoices and emails.</Field.Description>
</Field.Field>

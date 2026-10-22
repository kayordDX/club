<script lang="ts">
	import { Button } from "@kayord/ui";
	import { createAppForm } from "#lib/components/Form/index.js";
	import { z } from "zod";

	let { onselect }: { onselect: (_guest: { guestName: string; guestEmail: string | null; guestPhone: string | null }) => void } = $props();
	const schema = z.object({
		guestName: z.string().trim().min(1, "Enter the guest's name.").max(200),
		guestEmail: z.union([z.string().trim().email().max(256), z.literal("")]),
		guestPhone: z.string().trim().max(50),
	});
	const form = createAppForm(() => ({
		defaultValues: { guestName: "", guestEmail: "", guestPhone: "" },
		validators: { onChange: schema },
		onSubmit: ({ value }) => {
			const guest = schema.parse(value);
			onselect({ ...guest, guestEmail: guest.guestEmail || null, guestPhone: guest.guestPhone || null });
		},
	}));
</script>

<form
	class="max-w-2xl space-y-4"
	onsubmit={(event) => {
		event.preventDefault();
		form.handleSubmit();
	}}
>
	<form.AppField name="guestName">
		{#snippet children(field)}<field.Input label="Guest name" autocomplete="name" />{/snippet}
	</form.AppField>
	<form.AppField name="guestEmail">
		{#snippet children(field)}<field.Input label="Guest email (optional)" type="email" autocomplete="email" />{/snippet}
	</form.AppField>
	<form.AppField name="guestPhone">
		{#snippet children(field)}<field.Input label="Guest phone (optional)" type="tel" autocomplete="tel" />{/snippet}
	</form.AppField>
	<Button type="submit">Use guest details</Button>
</form>

<script lang="ts">
	import { BOOKING_STATUS_OPTIONS, statusLabel } from "$lib/booking/status";
	import { BookingStatusEnum } from "$lib/api";
	import { Alert, Button, Dialog, Select } from "@kayord/ui";

	let {
		status,
		disabled = false,
		onChange,
	}: {
		status: BookingStatusEnum;
		disabled?: boolean;
		onChange: (_status: BookingStatusEnum) => Promise<void>;
	} = $props();
	let openChangeStatusDialog = $state(false);
	let nextStatus = $state("");
	let saving = $state(false);
	let error = $state("");

	async function save() {
		if (saving || !nextStatus || Number(nextStatus) === status) return;
		saving = true;
		error = "";
		try {
			await onChange(Number(nextStatus) as BookingStatusEnum);
			openChangeStatusDialog = false;
		} catch (cause) {
			error = cause instanceof Error ? cause.message : "Unable to change status. Please try again.";
		} finally {
			saving = false;
		}
	}
</script>

<Button
	variant="outline"
	size="sm"
	{disabled}
	onclick={() => {
		nextStatus = String(status);
		error = "";
		openChangeStatusDialog = true;
	}}>Change status</Button
>

<Dialog.Root bind:open={openChangeStatusDialog}>
	<Dialog.Content
		onInteractOutside={(event) => {
			if (saving) event.preventDefault();
		}}
		onEscapeKeydown={(event) => {
			if (saving) event.preventDefault();
		}}
		showCloseButton={!saving}
	>
		<Dialog.Header>
			<Dialog.Title>Change booking status</Dialog.Title>
			<Dialog.Description>Current status: {statusLabel(status)}. Changing status does not change the payment balance.</Dialog.Description>
		</Dialog.Header>
		<Select.Root type="single" bind:value={nextStatus} disabled={saving}>
			<Select.Trigger aria-label="New booking status">{statusLabel(Number(nextStatus))}</Select.Trigger>
			<Select.Content>
				{#each BOOKING_STATUS_OPTIONS as option (option.value)}
					<Select.Item value={String(option.value)} label={option.label}>{option.label}</Select.Item>
				{/each}
			</Select.Content>
		</Select.Root>
		{#if nextStatus === String(BookingStatusEnum.Cancelled)}
			<p class="text-muted-foreground text-sm">Cancelling releases the reserved places. Player details are kept.</p>
		{/if}
		{#if error}
			<Alert.Root variant="destructive"><Alert.Title>Status not changed</Alert.Title><Alert.Description>{error}</Alert.Description></Alert.Root>
		{/if}
		<Dialog.Footer>
			<Button variant="outline" disabled={saving} onclick={() => (openChangeStatusDialog = false)}>Cancel</Button>
			<Button disabled={saving || !nextStatus || Number(nextStatus) === status} onclick={save}>{saving ? "Saving..." : "Save status"}</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

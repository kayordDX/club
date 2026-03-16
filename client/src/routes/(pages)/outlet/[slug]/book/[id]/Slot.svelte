<script lang="ts">
	import { Button, Input, Popover, StatusDot, Table } from "@kayord/ui";
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { type SlotGetAllResponse, createSlotGetContracts } from "$lib/api";
	import { Card } from "@kayord/ui";
	import { ChevronRightIcon, CircleQuestionMark, MinusIcon, PlusIcon } from "@lucide/svelte";

	type Props = {
		slot: SlotGetAllResponse;
		selectedDate: string;
	};

	let { slot, selectedDate }: Props = $props();

	let slotCount = $state(1);
	let slotContractEnabled = $state(false);
	const available = $derived(slot.total - slot.booked);
	const bookHref = $derived.by(() => {
		const params = new URLSearchParams({
			slotId: slot.id,
			quantity: String(slotCount),
			date: selectedDate,
		});

		return resolve(`/outlet/${page.params.slug}/book/${page.params.id}/confirm?${params.toString()}`);
	});

	const contractsQuery = createSlotGetContracts(
		() => slot.id,
		() => ({ query: { enabled: slotContractEnabled } })
	);

	const formatTime = (datetime?: string | undefined | null) => {
		if (!datetime) return "";
		return new Date(datetime).toLocaleTimeString("en-US", {
			hour: "2-digit",
			minute: "2-digit",
			hour12: false,
		});
	};
</script>

<Card.Root class="flex h-full w-full flex-row items-center gap-6 p-4">
	<div class="flex h-full flex-col justify-between gap-2">
		<div class="text-muted-foreground text-xs">Resource</div>
		<div class="font-bold">
			{slot.resourceName}
		</div>
	</div>
	<div class="flex h-full flex-col justify-between gap-2">
		<div class="text-muted-foreground text-xs">Time</div>
		<div class="font-bold">
			{formatTime(slot.startDatetime)}
			<span class="text-muted-foreground">-</span>
			{formatTime(slot.endDatetime)}
		</div>
	</div>

	{#if slot.total > 1}
		{@const booked = slot.booked}
		<div class="flex h-full flex-col justify-between gap-2">
			<div class="text-muted-foreground text-xs">Slots</div>

			<div class="mb-1.5 flex items-center gap-1">
				{#each Array(available) as _, i (i)}
					<StatusDot.Root variant="success" />
				{/each}
				{#each Array(booked) as _, i (i)}
					<StatusDot.Root variant="error" />
				{/each}
			</div>
		</div>
	{/if}
	{#if slot.total > 1}
		<div class="flex h-full flex-col justify-between gap-2">
			<div class="text-muted-foreground text-xs">Players</div>
			<div class="flex items-center gap-1">
				<Button
					size="icon"
					variant="outline"
					class="size-7"
					onclick={() => slotCount--}
					disabled={slotCount <= 1}
				>
					<MinusIcon class="size-3" />
				</Button>
				<Input class="h-7 w-10 p-0 text-center" bind:value={slotCount} />
				<Button
					size="icon"
					variant="outline"
					class="size-7"
					onclick={() => slotCount++}
					disabled={slotCount >= available}
				>
					<PlusIcon class="size-3" />
				</Button>
			</div>
		</div>
	{/if}
	<div class="flex h-full flex-col justify-center gap-2">
		<Popover.Root bind:open={slotContractEnabled}>
			<Popover.Trigger>
				{#snippet child({ props })}
					<Button {...props} variant="ghost" size="icon" class="size-8">
						<CircleQuestionMark class="text-primary size-4" />
					</Button>
				{/snippet}
			</Popover.Trigger>
			<Popover.Content class="p-0">
				<Table.Root class="rounded-md">
					<Table.Header>
						<Table.Row>
							<Table.Head>Type</Table.Head>
							<Table.Head>Price</Table.Head>
						</Table.Row>
					</Table.Header>
					<Table.Body>
						{#each contractsQuery.data ?? [] as contract (contract.id)}
							<Table.Row>
								<Table.Cell>{contract.contractName} ({contract.description})</Table.Cell>
								<Table.Cell>R{contract.price.toFixed(2)}</Table.Cell>
							</Table.Row>
						{/each}
					</Table.Body>
				</Table.Root>
			</Popover.Content>
		</Popover.Root>
	</div>
	<div class="flex h-full flex-col justify-center gap-2">
		<Button variant="outline" href={bookHref} disabled={available <= 0}>
			<ChevronRightIcon /> Book
		</Button>
	</div>
</Card.Root>

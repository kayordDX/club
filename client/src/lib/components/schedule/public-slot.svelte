<script lang="ts">
	import { Button, Input, Popover, StatusDot, Table } from "@kayord/ui";
	import { type SlotGetAllResponse } from "$lib/api";
	import { slotGetContracts } from "$lib/api/remote/slot.remote";
	import { Card } from "@kayord/ui";
	import { ChevronRightIcon, CircleDotIcon, CircleQuestionMark, ClockIcon, MinusIcon, PlusIcon } from "@lucide/svelte";

	type Props = {
		slot: SlotGetAllResponse;
		selectedDate: string;
		onBook: (..._args: [string, number, string]) => void;
	};

	let { slot, selectedDate, onBook }: Props = $props();

	let slotCount = $state(1);
	let slotContractEnabled = $state(false);
	const available = $derived(Math.max(0, slot.total - slot.booked));
	const isUnavailable = $derived(available <= 0 || !slot.isEnabled || !slot.isAvailable);

	const formatTime = (datetime?: string | undefined | null) => {
		if (!datetime) return "";
		return new Date(datetime).toLocaleTimeString("en-US", {
			hour: "2-digit",
			minute: "2-digit",
			hour12: false,
		});
	};
</script>

<Card.Root class={["w-full min-w-0 gap-0 p-0", isUnavailable && "opacity-60"]} aria-disabled={isUnavailable}>
	<Card.Header class="bg-muted/40 border-b p-4">
		<div class="flex flex-wrap items-center justify-between gap-2">
			<div class="flex items-center gap-1">
				<ClockIcon class="text-muted-foreground size-4" />
				<div class="text-base font-semibold tabular-nums">
					{formatTime(slot.startDatetime)}
					<span class="text-muted-foreground">-</span>
					{formatTime(slot.endDatetime)}
				</div>
			</div>
			<div class="bg-background flex items-center gap-1.5 rounded-md border px-2 py-1 text-xs">
				<CircleDotIcon class="text-muted-foreground size-4" />
				<div class="font-bold">
					{slot.resourceName}
				</div>
			</div>
		</div>
	</Card.Header>
	<Card.Content class="flex flex-wrap items-end justify-between gap-4 p-4">
		<div class="flex w-full items-center justify-between gap-2">
			<p class="text-muted-foreground text-xs">
				{!slot.isEnabled
					? "Slot unavailable"
					: available === 0
						? "Fully booked"
						: !slot.isAvailable
							? "Slot unavailable"
							: `${available} of ${slot.total} available`}
			</p>
			{#if slot.total > 1}
				<div class="flex flex-wrap items-center gap-1" aria-hidden="true">
					{#each Array(available), i (i)}
						<StatusDot.Root variant="success" />
					{/each}
					{#each Array(slot.booked), i (i)}
						<StatusDot.Root variant="error" />
					{/each}
				</div>
			{/if}
		</div>

		{#if slot.total > 1}
			<div class="flex flex-col gap-1">
				<div class="text-muted-foreground text-xs">Players</div>
				<div class="flex items-center gap-1">
					<Button
						size="icon"
						variant="outline"
						class="size-8"
						aria-label="Fewer players"
						onclick={() => slotCount--}
						disabled={isUnavailable || slotCount <= 1}
					>
						<MinusIcon class="size-3" />
					</Button>
					<Input
						type="number"
						min={1}
						max={available}
						aria-label="Number of players"
						disabled={isUnavailable}
						class="h-8 w-12 p-0 text-center"
						bind:value={slotCount}
					/>
					<Button
						size="icon"
						variant="outline"
						class="size-8"
						aria-label="More players"
						onclick={() => slotCount++}
						disabled={isUnavailable || slotCount >= available}
					>
						<PlusIcon class="size-3" />
					</Button>
				</div>
			</div>
		{/if}
		<div class="ml-auto flex flex-col items-center justify-center gap-2">
			<div class="flex flex-col gap-1">
				<div class="text-muted-foreground text-center text-xs">Price</div>
				<Popover.Root bind:open={slotContractEnabled}>
					<Popover.Trigger>
						{#snippet child({ props })}
							<Button {...props} variant="ghost" size="icon" class="size-8" aria-label="View prices">
								<CircleQuestionMark class="text-primary size-4" />
							</Button>
						{/snippet}
					</Popover.Trigger>
					<Popover.Content class="p-0">
						{#if slotContractEnabled}
							<!-- Scoped boundary so this lazy load doesn't blank the page -->
							<svelte:boundary>
								{#snippet failed()}<p class="p-4 text-sm">Unable to load prices</p>{/snippet}
								{#snippet pending()}<p class="p-4 text-sm">Loading…</p>{/snippet}
								{@const contracts = await slotGetContracts(slot.id)}
								<Table.Root class="rounded-md">
									<Table.Header>
										<Table.Row>
											<Table.Head>Type</Table.Head>
											<Table.Head>Price</Table.Head>
										</Table.Row>
									</Table.Header>
									<Table.Body>
										{#each contracts as contract (contract.id)}
											<Table.Row>
												<Table.Cell>{contract.contractName} ({contract.description})</Table.Cell>
												<Table.Cell>R{contract.price.toFixed(2)}</Table.Cell>
											</Table.Row>
										{/each}
									</Table.Body>
								</Table.Root>
							</svelte:boundary>
						{/if}
					</Popover.Content>
				</Popover.Root>
			</div>
		</div>
		<div class="flex justify-end gap-2">
			<div class="flex flex-col gap-1">
				<div class="text-muted-foreground text-center text-xs">Booking</div>
				<Button
					onclick={() => onBook(slot.id, slotCount, selectedDate)}
					disabled={isUnavailable || !Number.isInteger(slotCount) || slotCount < 1 || slotCount > available}
				>
					<ChevronRightIcon /> Book
				</Button>
			</div>
		</div>
	</Card.Content>
</Card.Root>

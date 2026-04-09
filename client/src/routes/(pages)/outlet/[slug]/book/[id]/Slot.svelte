<script lang="ts">
	import { Button, Input, Popover, StatusDot, Table } from "@kayord/ui";
	import { page } from "$app/state";
	import { cn } from "@kayord/ui/utils";
	import { resolve } from "$app/paths";
	import { type SlotGetAllResponse, createSlotGetContracts, createSlotAvailable } from "$lib/api";
	import { Card } from "@kayord/ui";
	import {
		ChevronRightIcon,
		CircleDotIcon,
		CircleQuestionMark,
		ClockIcon,
		MinusIcon,
		PlusIcon,
	} from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { goto } from "$app/navigation";

	type Props = {
		slot: SlotGetAllResponse;
		selectedDate: string;
		refetch: () => void;
	};

	let { slot, selectedDate, refetch }: Props = $props();

	let slotCount = $state(1);
	let slotContractEnabled = $state(false);
	const available = $derived(slot.total - slot.booked);
	const isUnavailable = $derived(available <= 0);

	const availableMutation = createSlotAvailable();

	const checkAvailable = async (slot: SlotGetAllResponse) => {
		try {
			const isAvailable = await availableMutation.mutateAsync({
				data: {
					id: slot.id,
					slotCount: slotCount,
				},
			});
			if (isAvailable) {
				goto(
					resolve(
						`/outlet/${page.params.slug}/book/${page.params.id}/slot/${slot.id}?slotCount=${slotCount ?? 1}&date=${selectedDate}`
					)
				);
			} else {
				toast.error("Not enough slots available");
				refetch();
			}
		} catch {
			console.error("Check failed");
		}
	};

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

<Card.Root
	class={cn("w-full gap-0 p-0", isUnavailable && "pointer-events-none opacity-50")}
	aria-disabled={isUnavailable}
>
	<Card.Header class="bg-muted/50 p-2 ">
		<div class="flex items-center justify-start gap-4">
			<div class="flex items-center gap-1">
				<ClockIcon class="text-muted-foreground size-4" />
				<div class="font-bold">
					{formatTime(slot.startDatetime)}
					<span class="text-muted-foreground">-</span>
					{formatTime(slot.endDatetime)}
				</div>
			</div>
			<div class="flex items-center gap-1">
				<CircleDotIcon class="text-muted-foreground size-4" />
				<div class="font-bold">
					{slot.resourceName}
				</div>
			</div>
		</div>
	</Card.Header>
	<Card.Content class="flex flex-wrap content-start gap-2 p-2">
		<!-- <div class="text-muted-foreground text-xs">Slots</div>
		<div class="text-muted-foreground text-xs">Players</div>
		<div class="text-muted-foreground text-center text-xs">Price</div>
		<div class="text-muted-foreground text-xs"></div> -->
		{#if slot.total > 1}
			{@const booked = slot.booked}
			<div class="flex flex-col gap-1">
				<div class="text-muted-foreground text-xs">Slots</div>
				<div class="mb-1.5 flex grow flex-row items-center gap-1">
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
			<div class="flex flex-col gap-1">
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
		<div class="flex h-full flex-col items-center justify-center gap-2">
			<div class="flex flex-col gap-1">
				<div class="text-muted-foreground text-center text-xs">Price</div>
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
		</div>
		<div class="flex justify-end gap-2">
			<div class="flex flex-col gap-1">
				<div class="text-muted-foreground text-center text-xs">Action</div>
				<Button variant="outline" onclick={() => checkAvailable(slot)} disabled={available <= 0}>
					<ChevronRightIcon /> Book
				</Button>
			</div>
		</div>
	</Card.Content>
</Card.Root>

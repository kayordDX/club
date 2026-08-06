<script lang="ts">
	import { Button, Card, Command, Table } from "@kayord/ui";
	import { CirclePlusIcon, TrashIcon } from "@lucide/svelte";

	import { extraGetFacility } from "$lib/api/remote/extra.remote";
	import type { ExtraDTO } from "$lib/api";
	import type { SelectedExtra } from "$lib/booking/schema";
	import ExtrasAmount from "./ExtrasAmount.svelte";

	let open = $state(false);

	type Props = {
		facilityId: number;
		selectedExtras: Array<SelectedExtra>;
	};

	let { facilityId, selectedExtras = $bindable() }: Props = $props();

	const selectItem = (id: number, extras: ExtraDTO[]) => {
		const extra = extras.find((item) => item.id === id);
		if (extra == undefined) {
			open = false;
			return;
		}

		selectedExtras = selectedExtras.some((item) => item.id === id)
			? selectedExtras
			: [...selectedExtras, { id, amount: 1, name: extra.name, price: extra.price }];
		open = false;
	};
</script>

<svelte:boundary>
	{@const extras = await extraGetFacility(facilityId)}

	<Button onclick={() => (open = true)}>
		<CirclePlusIcon /> Add Extra
	</Button>

	{#if selectedExtras.length > 0}
		<Card.Root class="mt-4 p-0">
			<Table.Root>
				<Table.Header>
					<Table.Row>
						<Table.Head>Extra</Table.Head>
						<Table.Head>Price</Table.Head>
						<Table.Head>Amount</Table.Head>
						<Table.Head class="text-end"></Table.Head>
					</Table.Row>
				</Table.Header>
				<Table.Body>
					{#each selectedExtras as extra, index (extra.id)}
						<Table.Row>
							<Table.Cell>{extra.name}</Table.Cell>
							<Table.Cell class="font-bold">R {extra.price.toFixed(2)}</Table.Cell>
							<Table.Cell>
								<ExtrasAmount bind:selectedExtras {index} />
							</Table.Cell>
							<Table.Cell class="text-end">
								<Button size="icon" variant="destructive" onclick={() => (selectedExtras = selectedExtras.filter((_, i) => i !== index))}>
									<TrashIcon />
								</Button>
							</Table.Cell>
						</Table.Row>
					{/each}
				</Table.Body>
			</Table.Root>
		</Card.Root>
	{/if}

	<Command.Dialog bind:open>
		<Command.Input placeholder="Type a command or search..." />
		<Command.List>
			<Command.Empty>No results found.</Command.Empty>
			<Command.Group>
				{#each extras as item (item.id)}
					<Command.Item onSelect={() => selectItem(item.id, extras)}>
						<div class="flex w-full flex-row items-center justify-between">
							<div class="overflow-hidden text-ellipsis whitespace-nowrap">
								{item.name}
							</div>
							<div class="ml-2 flex flex-shrink-0 text-xs">R {item.price.toFixed(2)}</div>
						</div>
					</Command.Item>
				{/each}
			</Command.Group>
		</Command.List>
	</Command.Dialog>
</svelte:boundary>

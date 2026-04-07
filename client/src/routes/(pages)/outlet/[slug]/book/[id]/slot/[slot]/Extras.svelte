<script lang="ts">
	import { Button, Command } from "@kayord/ui";
	import { CirclePlusIcon } from "@lucide/svelte";

	import { createExtraGetFacility } from "$lib/api";
	import type { SelectedExtra } from "./schema";
	import { arrayUnique } from "$lib/util";

	let open = $state(false);

	type Props = {
		facilityId: number;
		selectedExtras: Array<SelectedExtra>;
	};

	let { facilityId, selectedExtras = $bindable() }: Props = $props();

	const extraQuery = createExtraGetFacility(() => facilityId);
	const data = $derived(extraQuery.data ?? []);

	const getExtraById = (id: number) => data.find((item) => item.id === id);

	const selectItem = (id: number) => {
		const extra = getExtraById(id);
		if (extra == undefined) {
			open = false;
			return;
		}

		if (selectedExtras.find((item) => item.id === id)) {
			open = false;
			return;
		}

		selectedExtras.push({ id, amount: 1, name: extra?.name, price: extra?.price });
		open = false;
	};
</script>

<Button onclick={() => (open = true)}>
	<CirclePlusIcon /> Add Extra
</Button>

<Command.Dialog bind:open>
	<Command.Input placeholder="Type a command or search..." />
	<Command.List>
		<Command.Empty>No results found.</Command.Empty>
		<Command.Group>
			{#each data as item (item.id)}
				<Command.Item onSelect={() => selectItem(item.id)}>
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

{JSON.stringify(selectedExtras)}

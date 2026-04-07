<script lang="ts">
	import { Button, Input } from "@kayord/ui";
	import { MinusIcon, PlusIcon } from "@lucide/svelte";
	import type { SelectedExtra } from "./schema";

	type Props = {
		selectedExtras: Array<SelectedExtra>;
		index: number;
	};

	let { selectedExtras = $bindable(), index }: Props = $props();
</script>

<Button
	size="icon"
	variant="outline"
	class="size-7"
	onclick={() => selectedExtras[index].amount--}
	disabled={selectedExtras[index].amount <= 1}
>
	<MinusIcon class="size-3" />
</Button>
<Input class="h-7 w-10 p-0 text-center" bind:value={selectedExtras[index].amount} />
<Button size="icon" variant="outline" class="size-7" onclick={() => selectedExtras[index].amount++}>
	<PlusIcon class="size-3" />
</Button>

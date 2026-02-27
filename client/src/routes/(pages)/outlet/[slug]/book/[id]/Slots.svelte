<script lang="ts">
	import { type SlotGetResponse } from "$lib/api";
	type Props = {
		slots: SlotGetResponse[];
	};

	let { slots }: Props = $props();
</script>

{JSON.stringify(slots)}

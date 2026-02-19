<script lang="ts">
	import GolfIcon from "$lib/svg/icons/GolfIcon.svelte";
	import TennisBallIcon from "$lib/svg/icons/TennisBallIcon.svelte";
	import { CircleSmallIcon } from "@lucide/svelte";

	type Props = {
		typeId: number;
	};

	let { typeId }: Props = $props();
</script>

{#if typeId == 1}
	<GolfIcon />
{:else if typeId == 2}
	<TennisBallIcon />
{:else}
	<CircleSmallIcon />
{/if}

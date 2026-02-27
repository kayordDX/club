<script lang="ts">
	import GolfIcon from "$lib/svg/icons/GolfIcon.svelte";
	import TennisBallIcon from "$lib/svg/icons/TennisBallIcon.svelte";
	import { cn } from "@kayord/ui/utils";
	import { CircleSmallIcon } from "@lucide/svelte";

	type Props = {
		typeId: number;
		class?: string;
	};

	let { typeId, class: className }: Props = $props();
</script>

{#if typeId == 1}
	<GolfIcon class={cn("text-green-500 dark:text-green-300", className)} />
{:else if typeId == 2}
	<TennisBallIcon class={cn("text-yellow-500 dark:text-yellow-300", className)} />
{:else}
	<CircleSmallIcon class={cn(className)} />
{/if}

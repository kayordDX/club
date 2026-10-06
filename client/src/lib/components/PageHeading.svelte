<script lang="ts">
	import type { LucideIcon } from "@lucide/svelte";

	type Props = {
		title: string;
		description: string;
		icon?: LucideIcon;
	};

	let { title, description, icon }: Props = $props();

	const HeadingIcon = $derived(icon);
</script>

<div class="flex flex-col gap-2">
	<div class="flex items-center gap-2 text-2xl">
		{#if icon}
			<div class="bg-muted text-muted-foreground flex size-10 items-center justify-center rounded-md">
				<HeadingIcon class="size-5" />
			</div>
		{/if}
		{title}
	</div>
	<div>
		<div class="text-muted-foreground text-sm">{description}</div>
	</div>
</div>

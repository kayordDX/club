<script lang="ts">
	import { goto } from "$app/navigation";
	import { cn } from "@kayord/ui/utils";
	import type { Snippet } from "svelte";
	import Menu from "./Menu.svelte";

	interface Props {
		children?: Snippet;
		class?: string;
		leftHeader?: Snippet;
	}

	let { children, class: className, leftHeader }: Props = $props();
</script>

<div
	class={cn(
		"bg-secondary border-secondary flex h-14 items-center justify-between border-b-2 p-2",
		className
	)}
>
	<button class="flex items-center" onclick={() => goto("/")}>
		<img src={`/favicon.svg`} alt="logo" class="h-10" />
		<span class="hidden text-lg font-bold md:block">Online</span>
	</button>

	{#if leftHeader}
		{@render leftHeader()}
	{/if}

	<span>
		{#if children}
			{@render children()}
		{/if}
	</span>

	<Menu />
</div>

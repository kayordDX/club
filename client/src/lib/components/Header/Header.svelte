<script lang="ts">
	import { goto } from "$app/navigation";
	import { cn } from "@kayord/ui/utils";
	import type { Snippet } from "svelte";
	import UserMenu from "./UserMenu.svelte";
	import { LightSwitch } from "@kayord/ui";

	interface Props {
		children?: Snippet;
		class?: string;
		leftHeader?: Snippet;
	}

	let { children, class: className, leftHeader }: Props = $props();
</script>

<div
	class={cn(
		"border-border bg-card/50 flex h-14 items-center justify-between border-b p-2 backdrop-blur-sm",
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

	<span>
		<LightSwitch />
		<UserMenu />
	</span>
</div>

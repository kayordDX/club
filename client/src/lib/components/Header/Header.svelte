<script lang="ts">
	import { cn } from "@kayord/ui/utils";
	import type { Snippet } from "svelte";
	import UserMenu from "./UserMenu.svelte";
	import { LightSwitch } from "@kayord/ui";
	import LogoButton from "../LogoButton.svelte";

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
	{#if leftHeader}
		{@render leftHeader()}
	{:else}
		<LogoButton />
	{/if}

	<span>
		{#if children}
			{@render children()}
		{/if}
	</span>

	<span class="flex items-center gap-2">
		<LightSwitch />
		<UserMenu />
	</span>
</div>

<script lang="ts" generics="T">
	// Await wrapper for remote-function queries (Promises). Mirrors the old
	// Query.svelte (loading / error / empty / loaded) but for remote queries.
	import type { Snippet } from "svelte";
	import { Empty, Loader } from "@kayord/ui";
	import QueryError from "./QueryError.svelte";
	import { BirdIcon, type Icon } from "@lucide/svelte";

	type Props = {
		promise: Promise<T>;
		emptyText?: string;
		emptyTitle?: string;
		emptyIcon?: typeof Icon;
		children: Snippet<[T]>;
	};

	let { promise, emptyText, emptyTitle, emptyIcon, children }: Props = $props();

	const EmptyIcon = $derived(emptyIcon ?? BirdIcon);
</script>

{#await promise}
	<Loader class="my-4" />
{:then data}
	{#if Array.isArray(data) && data.length === 0}
		<Empty.Root>
			<Empty.Header>
				<Empty.Media variant="icon">
					<EmptyIcon />
				</Empty.Media>
				<Empty.Title>{emptyTitle ?? "No Items"}</Empty.Title>
				<Empty.Description>
					{emptyText ?? "No items available"}
				</Empty.Description>
			</Empty.Header>
			<Empty.Content></Empty.Content>
		</Empty.Root>
	{:else}
		{@render children(data)}
	{/if}
{:catch err}
	<QueryError description={(err as Error)?.message} />
{/await}

<script lang="ts">
	import { Empty, Loader } from "@kayord/ui";
	import type { QueryObserverResult } from "@tanstack/svelte-query";
	import QueryError from "./QueryError.svelte";
	import type { Snippet } from "svelte";
	import { getError } from "$lib/types";
	import { BirdIcon, Icon } from "@lucide/svelte";
	type Props<TData = unknown, TError = unknown> = {
		query: QueryObserverResult<TData, TError>;
		emptyText?: string;
		emptyTitle?: string;
		emptyIcon?: typeof Icon;
		children: Snippet;
	};

	let { query, emptyText, emptyIcon, emptyTitle, children }: Props = $props();

	const isEmpty = $derived(query.data && Array.isArray(query.data) && query.data.length === 0);

	const errorDescription = $derived(query.isError ? getError(query.error) : undefined);
	const EmptyIcon = $derived(emptyIcon ?? BirdIcon);
</script>

{#if query.isLoading}
	<Loader />
{:else if query.isError}
	<QueryError description={errorDescription?.message} />
{:else if isEmpty}
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
	{@render children?.()}
{/if}

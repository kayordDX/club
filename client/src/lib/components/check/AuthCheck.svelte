<script lang="ts">
	import type { Snippet } from "svelte";

	interface Props {
		children?: Snippet;
	}

	let { children }: Props = $props();

	import { createAccountMe } from "$lib/api";
	import { user } from "$lib/stores/user.svelte";
	import { getCookie } from "$lib/util";
	import { Loader } from "@kayord/ui";
	import { createQuery } from "@tanstack/svelte-query";

	const hasTokenCookie = getCookie("HAS_TOKEN") ? true : false;

	const me = createAccountMe({ query: { enabled: hasTokenCookie, refetchOnWindowFocus: false } });

	// const me = createAccountMe({ query: { enabled: hasTokenCookie, refetchOnWindowFocus: false } });
	const d = $derived(me.data);

	$effect(() => {
		user.isLoading = me.isLoading;
	});

	$effect(() => {
		if (d) {
			user.value = d;
		}
	});
</script>

{#if user.isLoading}
	<Loader class="text-primary absolute inset-0 m-auto" />
{:else}
	{@render children?.()}
{/if}

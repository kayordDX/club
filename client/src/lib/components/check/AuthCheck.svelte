<script lang="ts">
	import type { Snippet } from "svelte";

	interface Props {
		children?: Snippet;
	}

	let { children }: Props = $props();

	import { user } from "$lib/stores/user.svelte";
	import { getCookie } from "$lib/util";
	import { Loader } from "@kayord/ui";

	const hasTokenCookie = getCookie("HAS_TOKEN") ? true : false;

	$effect(() => {
		if (hasTokenCookie) {
			user.update();
		}
	});
</script>

{#if user.isLoading}
	<Loader class="text-primary absolute inset-0 m-auto" />
{:else}
	{@render children?.()}
{/if}

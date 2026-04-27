<script lang="ts">
	import type { Snippet } from "svelte";
	import { auth } from "$lib/stores/auth.svelte";
	import { Loader } from "@kayord/ui";
	import { page } from "$app/state";
	import { PUBLIC_APP_URL } from "$env/static/public";

	interface Props {
		children?: Snippet;
		isProtected?: boolean;
	}

	let { children, isProtected }: Props = $props();

	$effect(() => {
		if (isProtected) {
			if (!auth.isLoading && !auth.isAuthenticated) {
				window.location.href = `${PUBLIC_APP_URL}/login?redirect=${page.url.pathname}`;
			}
		}
	});
</script>

{#if auth.isLoading}
	<Loader class="text-primary absolute inset-0 m-auto" />
{:else}
	{@render children?.()}
{/if}

<script lang="ts">
	import type { Snippet } from "svelte";
	import { user } from "$lib/stores/user.svelte";
	import { getCookie } from "$lib/util";
	import { Loader } from "@kayord/ui";
	import { page } from "$app/state";
	import { PUBLIC_APP_URL } from "$env/static/public";

	interface Props {
		children?: Snippet;
		isProtected?: boolean;
	}

	let { children, isProtected }: Props = $props();

	const hasTokenCookie = getCookie("HAS_TOKEN") ? true : false;

	$effect(() => {
		if (hasTokenCookie) {
			user.update();
		}
	});

	$effect(() => {
		if (isProtected) {
			if (!user.isLoading && !hasTokenCookie) {
				window.location.href = `${PUBLIC_APP_URL}/login?redirect=${page.url.pathname}`;
			}
		}
	});
</script>

{#if user.isLoading}
	<Loader class="text-primary absolute inset-0 m-auto" />
{:else}
	{@render children?.()}
{/if}

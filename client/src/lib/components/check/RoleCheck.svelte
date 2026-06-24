<script lang="ts">
	import type { Snippet } from "svelte";
	import { auth } from "$lib/stores/auth.svelte";
	import { Alert } from "@kayord/ui";
	import { CircleAlertIcon } from "@lucide/svelte";

	interface Props {
		children?: Snippet;
		roles: string[];
	}

	let { children, roles }: Props = $props();
</script>

{#if auth.hasRoles(roles)}
	{@render children?.()}
{:else}
	<Alert.Root class="mt-8" variant="destructive">
		<CircleAlertIcon />
		<Alert.Title>Access Denied</Alert.Title>
		<Alert.Description>
			<p>You do not have permission to view this page.</p>
		</Alert.Description>
	</Alert.Root>
{/if}

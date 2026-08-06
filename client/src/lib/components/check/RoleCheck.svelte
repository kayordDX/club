<script lang="ts">
	import type { Snippet } from "svelte";
	import { Alert } from "@kayord/ui";
	import { CircleAlertIcon } from "@lucide/svelte";
	import { useRoles } from "$lib/auth";

	interface Props {
		children?: Snippet;
		roles: string[];
	}

	let { children, roles }: Props = $props();

	// Facility roles are provided via context by the outlet/admin layout load.
	const userRoles = useRoles();
	const allowed = $derived(roles.some((r) => userRoles.includes(r)));
</script>

{#if allowed}
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

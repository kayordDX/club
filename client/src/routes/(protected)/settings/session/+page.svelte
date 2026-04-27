<script lang="ts">
	import { createAccountSession, createAccountSessionRevokeAll } from "$lib/api";
	import { ShieldBanIcon, ShieldIcon } from "@lucide/svelte";
	import PageHeading from "../PageHeading.svelte";
	import Query from "$lib/components/Query.svelte";
	import Session from "./Session.svelte";
	import { Button } from "@kayord/ui";
	import { toast } from "svelte-sonner";

	const query = createAccountSession();

	const mutation = createAccountSessionRevokeAll();

	let isRevoking = $state(false);
	const revokeAll = async () => {
		try {
			isRevoking = true;
			await mutation.mutateAsync();
			toast.info("Successfully revoked all sessions");
			query.refetch();
		} catch {
			toast.error("Error revoking all sessions");
		} finally {
			isRevoking = false;
		}
	};
</script>

<div class="m-4">
	<PageHeading
		title="Sessions"
		description="Manage your active sessions across devices."
		icon={ShieldIcon}
	/>
	<Query {query} emptyText="No sessions found">
		<div class="space-y-2">
			<div class="flex items-center justify-between">
				<div class="text-muted-foreground mt-6 text-sm">
					Sessions ({query.data?.length})
				</div>
				{#if (query.data?.length ?? 0) > 0}
					<Button
						variant="ghost"
						size="sm"
						class="text-destructive hover:text-destructive"
						disabled={isRevoking}
						onclick={revokeAll}
					>
						<ShieldBanIcon />
						Revoke All
					</Button>
				{/if}
			</div>
			{#each query.data as session (session.id)}
				<Session {session} refetch={query.refetch} />
			{/each}
		</div>
	</Query>
</div>

<script lang="ts">
	import { createRefreshList } from "$lib/api";
	import { ShieldBanIcon, ShieldIcon } from "@lucide/svelte";
	import PageHeading from "../PageHeading.svelte";
	import Query from "$lib/components/Query.svelte";
	import Session from "./Session.svelte";
	import { Button } from "@kayord/ui";
	import { createRefreshRevokeAll } from "$lib/api";
	import { toast } from "svelte-sonner";

	const query = createRefreshList();

	const activeSession = $derived(query.data?.find((x) => x.isCurrent == true));
	const otherSessions = $derived(query.data?.filter((x) => x.isCurrent == false));

	const mutation = createRefreshRevokeAll();

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
			{#if activeSession}
				<div class="text-muted-foreground mt-6 text-sm">Current Session</div>
				<Session session={activeSession} refetch={query.refetch} />
			{/if}

			<div class="flex items-center justify-between">
				<div class="text-muted-foreground mt-6 text-sm">
					Other Sessions ({otherSessions?.length})
				</div>
				{#if (otherSessions?.length ?? 0) > 0}
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
			{#each otherSessions as session (session.id)}
				<Session {session} refetch={query.refetch} />
			{/each}
		</div>
	</Query>
</div>

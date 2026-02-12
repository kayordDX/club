<script lang="ts">
	import { createRefreshList } from "$lib/api";
	import { ShieldIcon } from "@lucide/svelte";
	import PageHeading from "../PageHeading.svelte";
	import Query from "$lib/components/Query.svelte";
	import Session from "./Session.svelte";
	const query = createRefreshList();

	const activeSession = $derived(query.data?.find((x) => x.isCurrent == true));
	const otherSessions = $derived(query.data?.filter((x) => x.isCurrent == false));
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
				<Session session={activeSession} />
			{/if}

			<div class="flex items-center justify-between">
				<div class="text-muted-foreground mt-6 text-sm">
					Other Sessions ({otherSessions?.length})
				</div>
			</div>
			{#each otherSessions as session (session.id)}
				<Session {session} />
			{/each}
		</div>
	</Query>
</div>

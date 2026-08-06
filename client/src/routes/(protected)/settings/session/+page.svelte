<script lang="ts">
	import { accountSession, accountSessionRevokeAll } from "$lib/api/remote/account.remote";
	import { ShieldBanIcon, ShieldIcon } from "@lucide/svelte";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import Session from "./Session.svelte";
	import { Button } from "@kayord/ui";
	import { toast } from "svelte-sonner";

	const sessions = await accountSession();

	let isRevoking = $state(false);
	const revokeAll = async () => {
		try {
			isRevoking = true;
			await accountSessionRevokeAll();
			toast.info("Successfully revoked all sessions");
			// Full navigation back through the server session (all sessions gone).
			window.location.href = "/";
		} catch {
			toast.error("Error revoking all sessions");
		} finally {
			isRevoking = false;
		}
	};
</script>

<div class="m-4">
	<PageHeading title="Sessions" description="Manage your active sessions across devices." icon={ShieldIcon} />
	<div class="space-y-2">
		<div class="flex items-center justify-between">
			<div class="text-muted-foreground mt-6 text-sm">
				Sessions ({sessions?.length})
			</div>
			{#if (sessions?.length ?? 0) > 0}
				<Button variant="ghost" size="sm" class="text-destructive hover:text-destructive" disabled={isRevoking} onclick={revokeAll}>
					<ShieldBanIcon />
					Revoke All
				</Button>
			{/if}
		</div>
		{#each sessions ?? [] as session (session.id)}
			<Session {session} />
		{/each}
	</div>
</div>

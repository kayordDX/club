<script lang="ts">
	import type { AccountSessionResponse } from "$lib/api";
	import { createAccountSessionRevoke } from "$lib/api";
	import { Button, Card, Tooltip } from "@kayord/ui";
	import { ClockCheckIcon, NetworkIcon, ShieldXIcon, TimerIcon } from "@lucide/svelte";
	import { toast } from "svelte-sonner";

	type Props = {
		session: AccountSessionResponse;
		refetch: () => void;
	};

	let { session, refetch }: Props = $props();

	const mutation = createAccountSessionRevoke();

	let isRevoking = $state(false);
	const revoke = async () => {
		try {
			if (session.id == null) return;
			isRevoking = true;
			await mutation.mutateAsync({ data: { id: session.id } });
			refetch();
			toast.info("Successfully revoked session");
		} catch {
			toast.error("Error revoking session");
		} finally {
			isRevoking = false;
		}
	};
</script>

<Card.Root class="flex flex-row items-center px-2 py-4">
	<div class="w-full">
		<div class="text-muted-foreground flex items-center gap-2 text-xs">
			<NetworkIcon class="size-3" />
			{session.ipAddress}
		</div>
		<div class="text-muted-foreground flex items-center gap-2 text-xs">
			<Tooltip.Root>
				<Tooltip.Trigger class="flex items-center gap-1">
					<ClockCheckIcon class="size-3" />
				</Tooltip.Trigger>
				<Tooltip.Content>Last Access</Tooltip.Content>
			</Tooltip.Root>
			{new Date(session.lastAccess ?? 0).toLocaleString()}
		</div>
		<div class="text-muted-foreground flex items-center gap-2 text-xs">
			<Tooltip.Root>
				<Tooltip.Trigger class="flex items-center gap-1">
					<TimerIcon class="size-3" />
				</Tooltip.Trigger>
				<Tooltip.Content>Start</Tooltip.Content>
			</Tooltip.Root>
			{new Date(session.start ?? 0).toLocaleString()}
		</div>
	</div>
	<div class="flex items-center">
		<Button variant="destructive" size="sm" disabled={isRevoking} onclick={revoke}>
			<ShieldXIcon /> Revoke
		</Button>
	</div>
</Card.Root>

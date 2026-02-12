<script lang="ts">
	import type { RefreshListResponse } from "$lib/api";
	import { Badge, Button, Card, Tooltip } from "@kayord/ui";
	import {
		ClockIcon,
		GamepadIcon,
		MonitorIcon,
		MonitorSmartphoneIcon,
		PhoneIcon,
		ShieldXIcon,
		TabletIcon,
		TvIcon,
		WatchIcon,
	} from "@lucide/svelte";

	type Props = {
		session: RefreshListResponse;
	};

	let { session }: Props = $props();

	const getShortenedBrowserVersion = (version: string) => {
		const parts = version.split(".");
		return parts.slice(0, 1).join(".");
	};
</script>

<Card.Root class="flex flex-row items-center px-2 py-4">
	<div
		class="bg-muted text-muted-foreground flex size-5 size-10 items-center justify-center rounded-md"
	>
		{#if session.device == "Mobile"}
			<PhoneIcon />
		{:else if session.device == "Tablet"}
			<TabletIcon />
		{:else if session.device == "Desktop"}
			<MonitorIcon />
		{:else if session.device == "Watch"}
			<WatchIcon />
		{:else if session.device == "TV"}
			<TvIcon />
		{:else if session.device == "Console"}
			<GamepadIcon />
		{:else}
			<MonitorSmartphoneIcon />
		{/if}
	</div>
	<div class="w-full">
		<div class="flex w-full items-center justify-between">
			<div class="flex items-center gap-1">
				{session.browser}
				<Tooltip.Root>
					<Tooltip.Trigger>{getShortenedBrowserVersion(session.browserVersion)}</Tooltip.Trigger>
					<Tooltip.Content>
						<p>{session.browserVersion}</p>
					</Tooltip.Content>
				</Tooltip.Root>
				on {session.platform}
			</div>
		</div>
		<div class="text-muted-foreground flex items-center gap-2 text-xs">
			<ClockIcon class="size-3" /> Expires at {new Date(session.expiresAtUtc).toLocaleString()}
		</div>
	</div>
	<div class="flex items-center">
		{#if session.isCurrent}
			<Badge>Current</Badge>
		{:else}
			<Button variant="destructive"><ShieldXIcon /> Revoke</Button>
		{/if}
	</div>
</Card.Root>

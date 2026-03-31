<script lang="ts">
	import { Badge } from "@kayord/ui";
	import { ClockAlertIcon } from "@lucide/svelte";

	type Props = {
		expiresAt: string;
	};

	let { expiresAt }: Props = $props();

	const getSecondsLeft = () => {
		const diff = new Date(expiresAt).getTime() - Date.now();
		return Math.max(0, Math.floor(diff / 1000));
	};

	let secondsLeft = $state(getSecondsLeft());

	$effect(() => {
		secondsLeft = getSecondsLeft();
		const interval = setInterval(() => {
			secondsLeft = getSecondsLeft();
			if (secondsLeft <= 0) clearInterval(interval);
		}, 1000);
		return () => clearInterval(interval);
	});

	const formatted = $derived.by(() => {
		if (secondsLeft <= 0) return null;
		const h = Math.floor(secondsLeft / 3600);
		const m = Math.floor((secondsLeft % 3600) / 60);
		const s = secondsLeft % 60;
		const mm = String(m).padStart(2, "0");
		const ss = String(s).padStart(2, "0");
		return h > 0 ? `${h}:${mm}:${ss}` : `${mm}:${ss}`;
	});

	const isUrgent = $derived(secondsLeft > 0 && secondsLeft <= 60);
</script>

<div class="flex items-center gap-2">
	<ClockAlertIcon class="text-muted-foreground size-4 shrink-0" />
	<span class="text-muted-foreground text-sm">Expires in</span>
	{#if formatted}
		<Badge variant={isUrgent ? "destructive" : "outline"} class="font-mono tabular-nums">
			{formatted}
		</Badge>
	{:else}
		<Badge variant="destructive">Expired</Badge>
	{/if}
</div>

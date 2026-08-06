<script lang="ts">
	import { afterNavigate } from "$app/navigation";
	import Header from "$lib/components/Header/Header.svelte";
	import { Alert, Badge, Button, Skeleton } from "@kayord/ui";
	import { CircleAlertIcon, FrownIcon } from "@lucide/svelte";
	import { type HttpError } from "@sveltejs/kit";

	let { children } = $props();

	// svelte-ignore non_reactive_update
	let resetBoundary: (() => void) | undefined;

	afterNavigate(() => {
		resetBoundary?.();
	});
</script>

<svelte:boundary>
	{#snippet failed(error, reset)}
		{@const _ = resetBoundary = reset}
		<Header />

		<div class="p-4">
			<Alert.Root variant="destructive">
				<CircleAlertIcon class="animate-pulse" />
				<Alert.Title class="flex items-center gap-1">
					Something went wrong!
					<FrownIcon class="size-4" />
				</Alert.Title>
				<Alert.Description>
					<Badge variant="destructive">{(error as HttpError).status}</Badge>
					<p class="border-l-destructive mt-4 border-l-2 pl-2">
						{(error as HttpError).body.message}
					</p>

					<Button onclick={reset} variant="destructive">Try Again</Button>
				</Alert.Description>
			</Alert.Root>
		</div>
	{/snippet}

	{#snippet pending()}
		<div class="m-4 space-y-3">
			<Skeleton class="h-8 w-1/3" />
			<Skeleton class="h-4 w-2/3" />
			<Skeleton class="h-24" />
		</div>
	{/snippet}

	{@render children?.()}
</svelte:boundary>

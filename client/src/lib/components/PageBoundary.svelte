<script lang="ts">
	import { afterNavigate } from "$app/navigation";
	import { Alert, Badge, Button } from "@kayord/ui";
	import { CircleAlertIcon, FrownIcon } from "@lucide/svelte";
	import { isHttpError } from "@sveltejs/kit";
	import type { Snippet } from "svelte";

	type Props = {
		children?: Snippet;
	};

	let { children }: Props = $props();

	// svelte-ignore non_reactive_update
	let resetBoundary: (() => void) | undefined;

	afterNavigate(({ shallow, type }) => {
		if (shallow && type === "goto") return;

		resetBoundary?.();
	});
</script>

<svelte:boundary>
	{#snippet failed(error, reset)}
		{@const _ = resetBoundary = reset}
		{@const details = isHttpError(error) ? { ...error.body, status: error.status } : (error as App.Error | undefined)}
		<div class="p-4">
			<Alert.Root variant="destructive">
				<CircleAlertIcon class="animate-pulse" />
				<Alert.Title class="flex items-center gap-1">
					Something went wrong!
					<FrownIcon class="size-4" />
				</Alert.Title>
				<Alert.Description>
					<Badge variant="destructive">{details?.status ?? 500}</Badge>
					<p class="border-l-destructive mt-4 border-l-2 pl-2">
						{details?.message ?? "An unexpected error occurred."}
					</p>

					<Button onclick={reset} variant="destructive">Try Again</Button>
				</Alert.Description>
			</Alert.Root>
		</div>
	{/snippet}

	{@render children?.()}
</svelte:boundary>

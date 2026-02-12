<script lang="ts">
	import { Alert } from "@kayord/ui";
	import { CircleAlertIcon } from "@lucide/svelte";

	type Props = {
		description?: string;
	};

	let { description }: Props = $props();
</script>

<Alert.Root variant="destructive">
	<CircleAlertIcon />
	<Alert.Title>Error!</Alert.Title>
	<Alert.Description>{description}</Alert.Description>
</Alert.Root>

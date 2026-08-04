<script lang="ts">
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Alert } from "@kayord/ui";
	import { ScrollTextIcon } from "@lucide/svelte";
</script>

<div class="m-4 flex flex-col gap-6">
	<PageHeading title="Facility Rules" description="Configure booking rules specific to this facility." icon={ScrollTextIcon} />

	<Alert.Root>
		<Alert.Title>Coming soon</Alert.Title>
		<Alert.Description>Facility-level booking rules will be configurable here.</Alert.Description>
	</Alert.Root>
</div>

<script lang="ts">
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Alert } from "@kayord/ui";
	import { BuildingIcon } from "@lucide/svelte";
</script>

<div class="m-4 flex flex-col gap-6">
	<PageHeading title="Facility" description="Manage this facility and its resources." icon={BuildingIcon} />

	<Alert.Root>
		<Alert.Title>Coming soon</Alert.Title>
		<Alert.Description>Facility settings will be available here. Use the Rules sub-page to manage booking rules.</Alert.Description>
	</Alert.Root>
</div>

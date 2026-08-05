<script lang="ts">
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Alert } from "@kayord/ui";
	import { ScrollTextIcon } from "@lucide/svelte";
</script>

<div class="m-4 flex flex-col gap-6">
	<PageHeading title="Outlet Rules" description="Configure booking rules that apply across this outlet." icon={ScrollTextIcon} />

	<Alert.Root>
		<Alert.Title>Coming soon</Alert.Title>
		<Alert.Description>Outlet-level booking rules will be configurable here.</Alert.Description>
	</Alert.Root>
</div>

<script lang="ts">
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Alert } from "@kayord/ui";
	import { StoreIcon } from "@lucide/svelte";
</script>

<div class="m-4 flex flex-col gap-6">
	<PageHeading title="Outlet" description="Manage outlet-wide configuration and branding." icon={StoreIcon} />

	<Alert.Root>
		<Alert.Title>Coming soon</Alert.Title>
		<Alert.Description>Outlet settings will be available here. Use the Rules sub-page to manage booking rules.</Alert.Description>
	</Alert.Root>
</div>

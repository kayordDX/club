<script lang="ts">
	import PageHeading from "$lib/components/PageHeading.svelte";
	import { Alert } from "@kayord/ui";
	import { CreditCardIcon } from "@lucide/svelte";
</script>

<div class="m-4 flex flex-col gap-6">
	<PageHeading title="Payments" description="Track and reconcile payments collected for this facility." icon={CreditCardIcon} />

	<Alert.Root>
		<Alert.Title>Coming soon</Alert.Title>
		<Alert.Description>Payment reporting and reconciliation tools will be available here.</Alert.Description>
	</Alert.Root>
</div>

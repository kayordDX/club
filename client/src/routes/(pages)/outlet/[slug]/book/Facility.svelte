<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import type { FacilityDTO } from "$lib/api";
	import FacilityIcon from "$lib/components/Icons/FacilityIcon.svelte";
	import { Badge, Card } from "@kayord/ui";

	type Props = {
		facility: FacilityDTO;
	};

	let { facility }: Props = $props();
</script>

<a href={resolve(`/outlet/${page.params.slug}/book/${facility.id}`)}>
	<Card.Root class="flex flex-row items-center p-2">
		<div class="bg-background flex size-10 items-center justify-center rounded-md">
			<FacilityIcon typeId={facility.facilityType.id} />
		</div>
		<div class="flex flex-col justify-center">
			<p>{facility.name}</p>
			<Badge variant="outline">{facility.facilityType.name}</Badge>
		</div>
	</Card.Root>
</a>

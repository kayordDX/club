<script lang="ts">
	import type { FacilityDTO } from "$lib/api";
	import FacilityIcon from "$lib/components/Icons/FacilityIcon.svelte";
	import { Card } from "@kayord/ui";

	type Props = {
		facility: FacilityDTO;
	};

	let { facility }: Props = $props();
</script>

<Card.Root>
	{facility.name}
	{facility.facilityType.name}
	<FacilityIcon typeId={facility.facilityType.id} />
</Card.Root>

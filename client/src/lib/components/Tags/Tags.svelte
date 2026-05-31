<script lang="ts">
	import { Badge } from "@kayord/ui";

	type Props = {
		tags: string;
	};

	let { tags }: Props = $props();

	const tagList = $derived(tags.split(",").map((tag) => tag.trim()));
</script>

<div class="flex min-w-0 flex-nowrap gap-2 overflow-hidden">
	{#each tagList as tag (tag)}
		<Badge class="shrink-0" variant="secondary">{tag.trim()}</Badge>
	{/each}
</div>

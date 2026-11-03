<script lang="ts">
	import { groupIssues } from "#lib/bulk-upload/group-issues.js";

	let { issues }: { issues: string[] } = $props();
	const groups = $derived(groupIssues(issues));
</script>

{#if issues.length}
	<section role="alert" class="border-destructive/50 rounded border p-4">
		<h2 class="text-destructive font-semibold">Issues ({issues.length})</h2>
		<div class="mt-3 space-y-4">
			{#each groups as group (group.title)}
				<div>
					<h3 class="font-medium">{group.title} ({group.issues.length})</h3>
					<ul class="text-muted-foreground ml-5 list-disc text-sm">
						{#each group.issues as issue, index (`${index}:${issue}`)}
							<li>{issue}</li>
						{/each}
					</ul>
				</div>
			{/each}
		</div>
	</section>
{/if}

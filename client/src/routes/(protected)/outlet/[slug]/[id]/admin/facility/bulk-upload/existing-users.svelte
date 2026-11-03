<script lang="ts">
	import { Accordion } from "@kayord/ui";
	import type { CheckedMembership } from "#lib/bulk-upload/existing-user.js";

	let { users }: { users: CheckedMembership[] } = $props();
</script>

{#if users.length}
	<Accordion.Root type="single" class="rounded border border-orange-400 px-4">
		<Accordion.Item value="existing-users" class="border-0">
			<Accordion.Trigger level={2}>Existing users ({users.length})</Accordion.Trigger>
			<Accordion.Content>
				<p>These accounts will not be recreated. Existing contract assignments will be skipped.</p>
				<ul>
					{#each users as item (item.row.line)}
						<li>
							Line {item.row.line}: {item.row.email} — contract {item.row.contractId}: {item.existing?.isExistingMember
								? "already assigned"
								: "will be assigned"}
						</li>
					{/each}
				</ul>
			</Accordion.Content>
		</Accordion.Item>
	</Accordion.Root>
{/if}

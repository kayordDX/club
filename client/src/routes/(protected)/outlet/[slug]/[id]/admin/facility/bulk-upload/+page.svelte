<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { Button, Input } from "@kayord/ui";
	import PageHeading from "#lib/components/PageHeading.svelte";
	import ExistingUsers from "./existing-users.svelte";
	import ImportIssues from "./import-issues.svelte";
	import { adminContractAddMember, adminContractCreateMember, adminContractGetAll, adminContractSearchMember } from "#lib/api/remote/admin.remote.js";
	import { checkExistingUser, type CheckedMembership } from "#lib/bulk-upload/existing-user.js";
	import { importMemberships } from "#lib/bulk-upload/import-members.js";
	import { parseMembershipCsv, type MembershipRow } from "#lib/bulk-upload/csv.js";
	import { validateMembershipRows } from "#lib/bulk-upload/validate.js";

	const facilityId = $derived(Number(page.params.id));
	const contracts = $derived(adminContractGetAll(facilityId));
	const contractsHref = $derived(resolve(`outlet/${page.params.slug}/${facilityId}/admin/contracts`));
	let rows = $state.raw<MembershipRow[]>([]);
	let checked = $state.raw<CheckedMembership[]>([]);
	const existingUsers = $derived(checked.filter((item) => item.existing));
	let issues = $state.raw<string[]>([]);
	let status = $state("");
	let busy = $state(false);
	let ready = $state(false);

	async function loadFile(event: Event) {
		const input = event.currentTarget as HTMLInputElement;
		const file = input.files?.[0];
		rows = [];
		checked = [];
		issues = [];
		status = "";
		ready = false;
		if (!file) return;
		if (!file.name.toLowerCase().endsWith(".csv")) {
			issues = ["Choose a CSV file."];
			input.value = "";
			return;
		}
		if (file.size > 5 * 1024 * 1024) {
			issues = ["The CSV must be no larger than 5 MB."];
			input.value = "";
			return;
		}
		try {
			rows = parseMembershipCsv(await file.text());
			status = `${rows.length} rows loaded. Check the file before importing.`;
		} catch (error) {
			issues = [error instanceof Error ? error.message : "Could not read CSV."];
		}
		input.value = "";
	}

	async function checkFile() {
		busy = true;
		ready = false;
		checked = [];
		try {
			await contracts.refresh();
			const currentContracts = await contracts;
			const today = new Date().toISOString().slice(0, 10);
			const found = validateMembershipRows(rows, currentContracts, today);
			const results: CheckedMembership[] = [];
			if (!found.length) {
				for (const [index, row] of rows.entries()) {
					status = `Checking existing users ${index + 1} of ${rows.length}…`;
					const search = adminContractSearchMember({ facilityId, id: row.contractId, params: { query: row.email } });
					await search.refresh();
					const existing = await search;
					try {
						results.push(checkExistingUser(row, existing));
					} catch (error) {
						found.push(error instanceof Error ? error.message : `Line ${row.line}: could not verify account.`);
					}
				}
			}
			checked = results;
			issues = found;
			ready = found.length === 0;
			status = ready ? "Checks passed. Review the contracts below, then import." : "Fix the issues and upload the CSV again.";
		} catch {
			issues = ["Could not complete the checks. No users were imported; please try again."];
			status = "";
		} finally {
			busy = false;
		}
	}

	async function importMembers() {
		if (!ready) return;
		busy = true;
		ready = false;
		issues = [];
		try {
			await contracts.refresh();
			const currentContracts = await contracts;
			const found = validateMembershipRows(rows, currentContracts, new Date().toISOString().slice(0, 10));
			if (found.length) {
				issues = found;
				status = "Contracts changed. No users were imported.";
				return;
			}
			const result = await importMemberships(
				checked,
				async ({ row, existing }) => {
					const contract = currentContracts.find((item) => item.id === row.contractId)!;
					if (existing) {
						await adminContractAddMember({ facilityId, id: row.contractId, body: { userId: existing.userId, endDate: contract.endDate } });
					} else {
						await adminContractCreateMember({
							facilityId,
							id: row.contractId,
							body: {
								firstName: row.firstName,
								lastName: row.lastName,
								email: row.email,
								phoneNumber: row.mobile || null,
								endDate: contract.endDate,
							},
						});
					}
				},
				(index) => (status = `Importing member ${index + 1} of ${rows.length}…`)
			);
			issues = result.failures;
			status = `Assigned ${result.completed} memberships; ${result.skipped} already assigned and skipped; ${result.failures.length} failed.`;
			rows = [];
			checked = [];
		} catch {
			issues = ["Could not load contracts. No users were imported; please check the CSV again."];
			status = "";
		} finally {
			busy = false;
		}
	}
</script>

<div class="space-y-6 p-6">
	<PageHeading title="Bulk Upload" description="Import members into existing facility contracts." />
	<p>
		Create contracts first on the <a class="text-primary underline" href={contractsHref}>Contracts page</a>. Use their IDs in the CSV. Account setup emails are
		sent only when enabled on the server. Otherwise, new users need another way to set their password.
	</p>
	<p>CSV columns: <code>First Name,Last Name,Mobile,Email,ContractId</code></p>
	{#if contracts.current?.length}
		<div>
			<h2 class="font-semibold">Facility contract IDs</h2>
			<ul class="text-muted-foreground text-xs">
				{#each contracts.current as contract (contract.id)}<li>{contract.id}: {contract.name}</li>{/each}
			</ul>
		</div>
	{/if}
	<label for="membership-file" class="block font-medium">Membership CSV</label>
	<Input id="membership-file" type="file" accept=".csv,text/csv" disabled={busy} onchange={loadFile} />
	{#if status}<p role="status">{status}</p>{/if}
	<ImportIssues {issues} />
	<ExistingUsers users={existingUsers} />
	<div class="flex gap-2">
		<Button disabled={busy || !rows.length || ready} onclick={checkFile}>Check CSV</Button>
		<Button disabled={busy || !ready} onclick={importMembers}>Import members</Button>
	</div>
</div>

import { membershipAction, type CheckedMembership } from "./existing-user";

export async function importMemberships(
	members: CheckedMembership[],
	assign: (member: CheckedMembership) => Promise<void>,
	onProgress: (index: number) => void
): Promise<{ completed: number; skipped: number; failures: string[] }> {
	let completed = 0;
	let skipped = 0;
	const failures: string[] = [];
	for (const [index, member] of members.entries()) {
		if (membershipAction(member.existing) === "skip") {
			skipped++;
			continue;
		}
		onProgress(index);
		try {
			await assign(member);
			completed++;
		} catch {
			failures.push(
				`Line ${member.row.line}: import failed for ${member.row.email}. This user may have been created; check the user and contract before retrying.`
			);
		}
	}
	return { completed, skipped, failures };
}

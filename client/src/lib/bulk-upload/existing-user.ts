import type { MembershipRow } from "./csv";

export type ExistingUser = { userId: string; email?: string | null; isExistingMember: boolean };
export type CheckedMembership = { row: MembershipRow; existing: ExistingUser | null };

export function membershipAction(existing: ExistingUser | null): "create" | "assign" | "skip" {
	return !existing ? "create" : existing.isExistingMember ? "skip" : "assign";
}

export function checkExistingUser(row: MembershipRow, existing: ExistingUser | null | undefined): CheckedMembership {
	if (existing && existing.email?.trim().toLowerCase() !== row.email.toLowerCase()) {
		throw new Error(`Line ${row.line}: the account found for ${row.email} has a different email. Resolve this before importing.`);
	}
	return { row, existing: existing ?? null };
}

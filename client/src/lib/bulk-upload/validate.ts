import type { MembershipRow } from "./csv";

const prohibitedNameCharacters = `<>&"$%!#?§;*~/\\|^=[]{}()`;
const hasProhibitedNameCharacters = (name: string) =>
	[...name].some((character) => prohibitedNameCharacters.includes(character) || /[\p{Cc}\u2028\u2029]/u.test(character));

export function validateMembershipRows(rows: MembershipRow[], contracts: { id: number; isActive: boolean; endDate: string }[], today: string): string[] {
	const issues: string[] = [];
	const counts = new Map<string, number>();
	for (const row of rows) {
		const email = row.email.toLowerCase();
		counts.set(email, (counts.get(email) ?? 0) + 1);
	}
	if (!rows.length) issues.push("The CSV contains no members.");
	for (const row of rows) {
		const prefix = `Line ${row.line}:`;
		if (!row.firstName || !row.lastName) issues.push(`${prefix} first and last name are required.`);
		if (hasProhibitedNameCharacters(row.firstName)) {
			issues.push(`${prefix} first name contains unsupported characters: ${row.firstName}. Correct it before importing ${row.email}.`);
		}
		if (hasProhibitedNameCharacters(row.lastName)) {
			issues.push(`${prefix} last name contains unsupported characters: ${row.lastName}. Correct it before importing ${row.email}.`);
		}
		if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(row.email)) {
			issues.push(row.email ? `${prefix} invalid email address: ${row.email}.` : `${prefix} email address is required.`);
		}
		if (row.email && (counts.get(row.email.toLowerCase()) ?? 0) > 1) issues.push(`${prefix} duplicate email in the CSV: ${row.email}.`);
		const mobile = row.mobile.trim();
		if (!mobile) {
			issues.push(`${prefix} mobile number is required for ${row.email}.`);
		} else if (!/^\+?[\d\s().-]+$/.test(mobile) || !/^\d{6,15}$/.test(mobile.replace(/\D/g, ""))) {
			issues.push(`${prefix} invalid mobile number: ${mobile} for ${row.email}. Use 6–15 digits, optionally with a leading + and separators.`);
		}
		const contract = contracts.find((item) => item.id === row.contractId);
		if (!contract) issues.push(`${prefix} contract ${row.contractId} does not belong to this facility.`);
		else if (!contract.isActive || contract.endDate.slice(0, 10) < today) issues.push(`${prefix} contract ${row.contractId} is inactive or expired.`);
	}
	return issues;
}

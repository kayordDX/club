import { expect, it } from "vitest";
import { groupIssues } from "./group-issues";

it("groups all issues by type without losing details or misclassifying email values", () => {
	const issues = [
		"Line 3: invalid mobile number: 123 for contract@example.com.",
		"Line 2: duplicate email in the CSV: mobile@example.com.",
		"Line 4: first name contains unsupported characters: A (B).",
		"Line 5: mobile number is required for jane@example.com.",
		"Line 6: import failed for jane@example.com.",
		"Line 7: contract 1 does not belong to this facility.",
		"Could not read CSV.",
	];
	expect(groupIssues(issues)).toEqual([
		{ title: "Name issues", issues: [issues[2]] },
		{ title: "Email issues", issues: [issues[1]] },
		{ title: "Mobile number issues", issues: [issues[0], issues[3]] },
		{ title: "Contract issues", issues: [issues[5]] },
		{ title: "Import failures", issues: [issues[4]] },
		{ title: "Other issues", issues: [issues[6]] },
	]);
	expect(groupIssues([])).toEqual([]);
});

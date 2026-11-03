export type IssueGroup = { title: string; issues: string[] };

const categories: { title: string; matches: RegExp }[] = [
	{ title: "Name issues", matches: /^first and last name|^(first|last) name/ },
	{ title: "Email issues", matches: /^(invalid email|email address|duplicate email|email already belongs)/ },
	{ title: "Mobile number issues", matches: /^(mobile number|invalid mobile number)/ },
	{ title: "Contract issues", matches: /^contract\b/ },
	{ title: "Import failures", matches: /^import failed\b/ },
];

export function groupIssues(issues: string[]): IssueGroup[] {
	const groups = new Map<string, string[]>();
	for (const issue of issues) {
		const detail = issue.replace(/^Line \d+:\s*/i, "").toLowerCase();
		const title = categories.find((category) => category.matches.test(detail))?.title ?? "Other issues";
		const group = groups.get(title) ?? [];
		group.push(issue);
		groups.set(title, group);
	}
	return [...categories.map(({ title }) => title), "Other issues"].filter((title) => groups.has(title)).map((title) => ({ title, issues: groups.get(title)! }));
}

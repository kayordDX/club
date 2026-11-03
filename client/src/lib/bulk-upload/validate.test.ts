import { describe, expect, it } from "vitest";
import { validateMembershipRows } from "./validate";

const row = { line: 2, firstName: "Jane", lastName: "Doe", mobile: "0123456789", email: "jane@example.com", contractId: 1 };
const contracts = [{ id: 1, isActive: true, endDate: "2030-01-01T00:00:00Z" }];

describe("membership validation", () => {
	it("accepts a valid row", () => {
		expect(validateMembershipRows([row], contracts, "2029-01-01")).toEqual([]);
	});
	it("reports parentheses in either name before importing", () => {
		const issues = validateMembershipRows([{ ...row, firstName: "J C (Japie)", lastName: "Doe (Smith)" }], contracts, "2029-01-01");
		expect(issues).toContain("Line 2: first name contains unsupported characters: J C (Japie). Correct it before importing jane@example.com.");
		expect(issues).toContain("Line 2: last name contains unsupported characters: Doe (Smith). Correct it before importing jane@example.com.");
		expect(validateMembershipRows([{ ...row, firstName: "Jane & John" }], contracts, "2029-01-01")[0]).toContain("unsupported characters");
		expect(validateMembershipRows([{ ...row, firstName: "José", lastName: "O'Neill-Smith" }], contracts, "2029-01-01")).toEqual([]);
	});
	it("reports duplicate emails ignoring case and missing contracts", () => {
		const issues = validateMembershipRows([row, { ...row, line: 3, email: "JANE@example.com", contractId: 2 }], contracts, "2029-01-01");
		expect(issues.filter((issue) => issue.includes("duplicate"))).toEqual([
			"Line 2: duplicate email in the CSV: jane@example.com.",
			"Line 3: duplicate email in the CSV: JANE@example.com.",
		]);
		expect(issues.some((issue) => issue.includes("does not belong"))).toBe(true);
	});
	it("rejects expired contracts and malformed emails", () => {
		const issues = validateMembershipRows([{ ...row, email: "bad" }], contracts, "2031-01-01");
		expect(issues).toHaveLength(2);
		expect(issues).toContain("Line 2: invalid email address: bad.");
	});
	it("checks mobile numbers before importing", () => {
		expect(validateMembershipRows([{ ...row, mobile: "+27 (82) 123-4567" }], contracts, "2029-01-01")).toEqual([]);
		expect(validateMembershipRows([{ ...row, mobile: "not a number" }], contracts, "2029-01-01")).toContain(
			"Line 2: invalid mobile number: not a number for jane@example.com. Use 6–15 digits, optionally with a leading + and separators."
		);
		expect(validateMembershipRows([{ ...row, mobile: "12345" }], contracts, "2029-01-01")[0]).toContain("invalid mobile number");
		expect(validateMembershipRows([{ ...row, mobile: "123456" }], contracts, "2029-01-01")).toEqual([]);
		expect(validateMembershipRows([{ ...row, mobile: "" }], contracts, "2029-01-01")).toContain("Line 2: mobile number is required for jane@example.com.");
		expect(validateMembershipRows([{ ...row, mobile: "   " }], contracts, "2029-01-01")).toContain("Line 2: mobile number is required for jane@example.com.");
	});
	it("reports a missing email without displaying an empty address", () => {
		expect(validateMembershipRows([{ ...row, email: "" }], contracts, "2029-01-01")).toContain("Line 2: email address is required.");
	});
});

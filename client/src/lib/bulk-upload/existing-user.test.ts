import { describe, expect, it } from "vitest";
import { checkExistingUser, membershipAction } from "./existing-user";

const row = { line: 2, firstName: "Jane", lastName: "Doe", email: "jane@example.com", mobile: "", contractId: 1 };

describe("existing membership checks", () => {
	it("recognizes a new user", () => {
		expect(checkExistingUser(row, null)).toEqual({ row, existing: null });
	});
	it("recognizes an existing user and membership case-insensitively", () => {
		const existing = { userId: "id", email: "JANE@example.com", isExistingMember: true };
		expect(checkExistingUser(row, existing)).toEqual({ row, existing });
	});
	it("creates new users, assigns existing users and skips existing memberships", () => {
		expect(membershipAction(null)).toBe("create");
		expect(membershipAction({ userId: "id", email: row.email, isExistingMember: false })).toBe("assign");
		expect(membershipAction({ userId: "id", email: row.email, isExistingMember: true })).toBe("skip");
	});
	it("does not match an account with a different or missing email", () => {
		expect(() => checkExistingUser(row, { userId: "id", email: "other@example.com", isExistingMember: false })).toThrow("different email");
		expect(() => checkExistingUser(row, { userId: "id", email: null, isExistingMember: false })).toThrow("different email");
	});
});

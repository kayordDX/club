import { describe, expect, it, vi } from "vitest";
import { importMemberships } from "./import-members";
import type { CheckedMembership } from "./existing-user";

function member(line: number, existing: CheckedMembership["existing"] = null): CheckedMembership {
	return { row: { line, firstName: "Jane", lastName: "Doe", mobile: "", email: `user${line}@example.com`, contractId: 1 }, existing };
}

describe("bulk membership import", () => {
	it("continues after failures, lists each failed email and skips existing memberships", async () => {
		const members = [member(2), member(3), member(4, { userId: "id", email: "user4@example.com", isExistingMember: true }), member(5), member(6)];
		const assign = vi.fn(async ({ row }: CheckedMembership) => {
			if (row.line === 2 || row.line === 5) throw new Error("failure");
		});
		const progress = vi.fn();
		const result = await importMemberships(members, assign, progress);
		expect(assign.mock.calls.map(([item]) => item.row.line)).toEqual([2, 3, 5, 6]);
		expect(result.completed).toBe(2);
		expect(result.skipped).toBe(1);
		expect(result.failures).toHaveLength(2);
		expect(result.failures[0]).toContain("user2@example.com");
		expect(result.failures[1]).toContain("user5@example.com");
		expect(progress).toHaveBeenCalledTimes(4);
	});
});

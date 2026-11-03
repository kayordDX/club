import { expect, it } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import ExistingUsers from "./existing-users.svelte";

it("shows the existing-user count and reveals details only when expanded", async () => {
	const row = { line: 2, firstName: "Jane", lastName: "Doe", mobile: "0123456789", email: "jane@example.com", contractId: 7 };
	render(ExistingUsers, {
		users: [
			{ row, existing: { userId: "1", email: row.email, isExistingMember: false } },
			{ row: { ...row, line: 3, email: "john@example.com" }, existing: { userId: "2", email: "john@example.com", isExistingMember: true } },
		],
	});
	const trigger = page.getByRole("button", { name: "Existing users (2)" });
	await expect.element(trigger).toHaveAttribute("aria-expanded", "false");
	await expect.element(page.getByText("Line 2: jane@example.com", { exact: false })).not.toBeVisible();
	await trigger.click();
	await expect.element(trigger).toHaveAttribute("aria-expanded", "true");
	await expect.element(page.getByText("Line 2: jane@example.com", { exact: false })).toBeVisible();
	await expect.element(page.getByText("already assigned", { exact: false })).toBeVisible();
	await trigger.click();
	await expect.element(trigger).toHaveAttribute("aria-expanded", "false");
});

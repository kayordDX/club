import { expect, it } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import ImportIssues from "./import-issues.svelte";

it("shows grouped issue counts and each issue", async () => {
	render(ImportIssues, {
		issues: ["Line 2: duplicate email in the CSV: jane@example.com.", "Line 3: mobile number is required for john@example.com."],
	});
	await expect.element(page.getByRole("heading", { name: "Issues (2)" })).toBeVisible();
	await expect.element(page.getByRole("heading", { name: "Email issues (1)" })).toBeVisible();
	await expect.element(page.getByRole("heading", { name: "Mobile number issues (1)" })).toBeVisible();
	await expect.element(page.getByText("Line 2: duplicate email in the CSV: jane@example.com.")).toBeVisible();
	await expect.element(page.getByText("Line 3: mobile number is required for john@example.com.")).toBeVisible();
});

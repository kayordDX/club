import { expect, it } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import StoreIdentityHarness from "./store-identity-harness.svelte";

it("preserves store slice identity and reacts to immutable replacements", async () => {
	render(StoreIdentityHarness);
	await expect.element(page.getByText("Same identity: true")).toBeVisible();
	await expect.element(page.getByText("First player", { exact: true })).toBeVisible();
	await page.getByRole("button", { name: "Update unrelated field" }).click();
	await expect.element(page.getByText("Same identity: true")).toBeVisible();
	await page.getByRole("button", { name: "Replace players" }).click();
	await expect.element(page.getByText("Replacement player", { exact: true })).toBeVisible();
	await expect.element(page.getByText("First player", { exact: true })).not.toBeInTheDocument();
	await expect.element(page.getByText("Same identity: true")).toBeVisible();
});

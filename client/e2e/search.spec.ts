import { expect, test } from "@playwright/test";

test("searching for ruimsig returns matching results", async ({ page }) => {
	await page.goto("/");
	// Wait for hydration so the form's submit handler is attached before interacting.
	await page.waitForLoadState("networkidle");

	await page.getByPlaceholder("Search clubs, sports or locations...").fill("ruimsig");
	await page.getByRole("button", { name: "Search" }).click();

	// The committed term is echoed above the results.
	await expect(page.getByText(/searching for “ruimsig”/)).toBeVisible();
	// Only the matching outlet card is returned.
	await expect(page.locator("[data-testid^='outlet-']")).toHaveCount(1);
	await expect(page.locator("[data-testid^='outlet-']").getByText("Ruimsig Country Club")).toBeVisible();
});

test("searching for an unknown term shows the no-results state", async ({ page }) => {
	await page.goto("/");
	// Wait for hydration so the form's submit handler is attached before interacting.
	await page.waitForLoadState("networkidle");

	await page.getByPlaceholder("Search clubs, sports or locations...").fill("zzzzzz");
	await page.getByRole("button", { name: "Search" }).click();

	await expect(page.getByTestId("no-results")).toBeVisible();
});

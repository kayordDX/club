import { expect, test } from "@playwright/test";

test("toggles between dark and light mode", async ({ page }) => {
	await page.goto("/");
	// Wait for hydration so the toggle's click handler is attached before interacting.
	await page.waitForLoadState("networkidle");

	// The app ships with dark mode as the default.
	await expect(page.locator("html")).toHaveClass(/dark/);

	const toggle = page.getByRole("button", { name: "Toggle theme" });

	// Switching to light mode removes the dark class from <html>.
	await toggle.click();
	await expect(page.locator("html")).not.toHaveClass(/dark/);

	// Toggling again returns to dark mode.
	await toggle.click();
	await expect(page.locator("html")).toHaveClass(/dark/);
});

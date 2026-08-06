import { describe, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import Page from "./+page.svelte";

vi.mock("$lib/api/remote/outlet.remote", () => ({
	outletGetAll: () => Promise.resolve({ items: [], pageNumber: 1, totalPages: 0, totalCount: 0, hasPreviousPage: false, hasNextPage: false }),
}));

describe("/+page.svelte", () => {
	it("should render h1", async () => {
		const screen = await render(Page, {});
		const heading = screen.getByRole("heading", { level: 1 });
		await expect.element(heading).toBeInTheDocument();
	});
});

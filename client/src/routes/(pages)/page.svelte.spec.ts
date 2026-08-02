import { QueryClient, QueryClientProvider } from "@tanstack/svelte-query";
import { describe, expect, it } from "vitest";
import { render } from "vitest-browser-svelte";
import Page from "./+page.svelte";

describe("/+page.svelte", () => {
	it("should render h1", async () => {
		const screen = await render(
			Page,
			{},
			{
				wrapper: QueryClientProvider,
				wrapperProps: { client: new QueryClient() },
			}
		);

		const heading = screen.getByRole("heading", { level: 1 });
		await expect.element(heading).toBeInTheDocument();
	});
});

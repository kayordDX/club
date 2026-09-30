import { render } from "svelte/server";
import { describe, expect, it, vi } from "vitest";
import BookingsPage from "./+page.svelte";

vi.mock("$app/environment", () => ({ browser: false }));
vi.mock("$app/state", () => ({ page: { params: { slug: "ruimsig-country-club", id: "1" } } }));
vi.mock("$app/paths", () => ({ resolve: (path: string) => path }));
vi.mock("$lib/api/remote/admin.remote", () => ({ adminBookingGetAll: vi.fn() }));
vi.mock("@tanstack/svelte-query", () => ({
	createQuery: vi.fn((options: () => unknown) => {
		// Query options are evaluated during SSR even when the query is disabled.
		options();
		return { data: undefined, isFetching: false, isError: false };
	}),
}));

import { createQuery } from "@tanstack/svelte-query";
import { adminBookingGetAll } from "$lib/api/remote/admin.remote";

describe("admin bookings SSR", () => {
	it("initializes pagination and sorting before evaluating query options", () => {
		const { body } = render(BookingsPage);

		expect(body).toContain("Bookings");
		expect(vi.mocked(createQuery).mock.results.at(-1)?.type).toBe("return");
		const options = vi.mocked(createQuery).mock.calls.at(-1)?.[0];
		expect(typeof options === "function" ? options() : undefined).toMatchObject({
			queryKey: ["admin-bookings", 1, { page: 1, pageSize: 10, sorts: "-slotStartDatetime" }],
			enabled: false,
		});
		expect(adminBookingGetAll).not.toHaveBeenCalled();
	});
});

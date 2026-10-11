import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page as browserPage } from "vitest/browser";
import FacilityPage from "./+page.svelte";

const mocks = vi.hoisted(() => ({
	isManager: true,
	goto: vi.fn(),
	available: vi.fn(),
}));

vi.mock("$app/state", () => ({ page: { params: { slug: "club", id: "7" } } }));
vi.mock("$app/navigation", () => ({ goto: mocks.goto, afterNavigate: vi.fn() }));
vi.mock("$app/paths", () => ({
	resolve: (path: string, params?: Record<string, string>) =>
		path.replace("/(protected)", "").replace(/\[([^\]]+)\]/g, (_, key: string) => params?.[key] ?? key),
	assets: "",
	base: "",
	asset: (path: string) => path,
}));
vi.mock("runed/kit", () => ({
	createSearchParamsSchema: () => ({}),
	useSearchParams: () => ({ date: "2026-08-02" }),
}));
vi.mock("$lib/auth", () => ({ hasRoles: () => mocks.isManager, useUser: () => ({ name: "Signed-in user" }) }));
vi.mock("svelte-sonner", () => ({ toast: { error: vi.fn() } }));
vi.mock("$lib/api/remote/admin.remote", () => ({
	adminSlotGetAll: () =>
		Object.assign(
			Promise.resolve([
				{
					id: "slot-1",
					resourceName: "Court A",
					startDatetime: "2026-08-02T10:00:00Z",
					endDatetime: "2026-08-02T11:00:00Z",
					total: 4,
					booked: 0,
					isAvailable: true,
					isEnabled: true,
					bookings: [],
				},
			]),
			{ loading: false, refresh: vi.fn() }
		),
}));
vi.mock("$lib/api/remote/slot.remote", () => ({
	slotAvailable: mocks.available,
	slotGetContracts: vi.fn(),
	slotGetAll: () =>
		Object.assign(
			Promise.resolve([
				{
					id: "slot-1",
					resourceName: "Court A",
					startDatetime: "2026-08-02T10:00:00Z",
					endDatetime: "2026-08-02T11:00:00Z",
					total: 4,
					booked: 0,
					isAvailable: true,
					isEnabled: true,
				},
			]),
			{ loading: false, refresh: vi.fn() }
		),
}));

beforeEach(() => {
	vi.clearAllMocks();
	mocks.isManager = true;
	mocks.available.mockResolvedValue(true);
});

it("manager slot actions require explicit customer selection in admin creation", async () => {
	render(FacilityPage);
	await browserPage.getByRole("button", { name: "Create booking", exact: true }).click();
	await expect.poll(() => mocks.goto.mock.calls[0]?.[0]).toBe("/outlet/club/7/admin/bookings/new?slotId=slot-1&date=2026-08-02&slotCount=1");
	expect(mocks.available).toHaveBeenCalledWith({ id: "slot-1", slotCount: 1 });
});

it("customer slot actions preserve the self-service creation route", async () => {
	mocks.isManager = false;
	render(FacilityPage);
	await browserPage.getByRole("button", { name: "Book", exact: true }).click();
	await expect.poll(() => mocks.goto.mock.calls[0]?.[0]).toBe("/outlet/club/7/slot/slot-1?slotCount=1&date=2026-08-02");
});

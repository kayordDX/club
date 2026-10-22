import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page } from "vitest/browser";
import { BookingStatusEnum, type BookingDTO } from "#lib/api/index.js";
import BookingPage from "./+page.svelte";
import { adminSlotGetAll } from "#lib/api/remote/admin.remote.js";
import { slotGetAll } from "#lib/api/remote/slot.remote.js";

const mocks = vi.hoisted(() => ({
	change: vi.fn(),
	refresh: vi.fn(),
	publicSlots: vi.fn(),
	adminSlots: vi.fn(),
	invalidate: vi.fn(),
	toast: vi.fn(),
	pathRefresh: vi.fn(),
}));
const booking: BookingDTO = {
	id: 42,
	bookingStatusId: BookingStatusEnum.Cancelled,
	bookingStatus: { id: 3, name: "Cancelled" },
	bookingStatusDate: "2026-08-01T10:00:00Z",
	expiresAt: "2026-08-01T10:10:00Z",
	isPaid: false,
	amountPaid: 25,
	amountOutstanding: 75,
	slotContractBookings: [
		{
			id: 1,
			slotContractId: 1,
			bookingId: 42,
			name: "Test Player",
			slotContract: {
				id: 1,
				slotId: "slot",
				contractId: 1,
				price: 100,
				canPayLater: true,
				slot: { id: "slot", maxBookings: 4, startDatetime: "2026-08-01T10:00:00Z", endDatetime: "2026-08-01T11:00:00Z" },
			},
		},
	],
	extraBookings: [],
};
vi.mock("$app/state", () => ({ page: { params: { slug: "club", id: "7", bookingId: "42" } } }));
vi.mock("$app/paths", () => ({ resolve: (path: string) => path, assets: "", base: "", asset: (path: string) => path }));
vi.mock("svelte-sonner", () => ({ toast: { success: mocks.toast } }));
vi.mock("@tanstack/svelte-query", () => ({
	createMutation: (options: () => { mutationFn: (value: unknown) => Promise<void> }) => ({
		mutateAsync: (value: unknown) => options().mutationFn(value),
		isPending: false,
	}),
	createQuery: () => ({ data: undefined, isFetching: false, isPending: false, isError: false, refetch: vi.fn() }),
	useQueryClient: () => ({ invalidateQueries: mocks.invalidate }),
}));
vi.mock("#lib/api/remote/admin.remote.js", () => ({
	adminBookingGet: () => Object.assign(Promise.resolve(booking), { refresh: mocks.refresh }),
	adminBookingGetPath: () => Object.assign(Promise.resolve(undefined), { refresh: mocks.pathRefresh }),
	adminBookingUpdate: vi.fn(),
	adminBookingGetPayments: vi.fn(),
	adminBookingAcceptPayment: vi.fn(),
	adminBookingUpdateStatus: mocks.change,
	adminBookingGetOptions: vi.fn(() => ({ refresh: vi.fn().mockResolvedValue(undefined) })),
	adminSlotGetAll: vi.fn(() => ({ refresh: mocks.adminSlots })),
}));
vi.mock("#lib/api/remote/facility.remote.js", () => ({ facilityPaymentMethods: vi.fn() }));
vi.mock("#lib/api/remote/slot.remote.js", () => ({ slotGetAll: vi.fn(() => ({ refresh: mocks.publicSlots })), slotGetContracts: vi.fn() }));

beforeEach(() => {
	vi.clearAllMocks();
	for (const mock of [mocks.change, mocks.refresh, mocks.publicSlots, mocks.adminSlots, mocks.invalidate, mocks.pathRefresh]) mock.mockResolvedValue(undefined);
});

async function restore() {
	render(BookingPage);
	await page.getByRole("button", { name: "Change status", exact: true }).click();
	await page.getByRole("button", { name: "New booking status" }).click();
	await page.getByRole("option", { name: "Confirmed", exact: true }).click();
	await page.getByRole("button", { name: "Save status" }).click();
}

it("refreshes booking details, cached list and public/admin availability after restoration", async () => {
	await restore();
	await expect.element(page.getByRole("dialog")).not.toBeInTheDocument();
	expect(mocks.change).toHaveBeenCalledExactlyOnceWith({ facilityId: 7, id: 42, body: { status: BookingStatusEnum.Confirmed } });
	expect(mocks.refresh).toHaveBeenCalledOnce();
	expect(mocks.publicSlots).toHaveBeenCalledOnce();
	expect(mocks.adminSlots).toHaveBeenCalledOnce();
	expect(adminSlotGetAll).toHaveBeenCalledWith({ facilityId: 7, params: { date: "2026-08-01T00:00:00.000Z" } });
	expect(slotGetAll).toHaveBeenCalledWith({ facilityId: 7, date: "2026-08-01T00:00:00.000Z" });
	expect(mocks.invalidate).toHaveBeenCalledWith({ queryKey: ["admin-bookings", 7] });
	expect(mocks.invalidate).toHaveBeenCalledWith({ queryKey: ["admin-slots", 7] });
	expect(mocks.invalidate).toHaveBeenCalledWith({ queryKey: ["admin-booking-payments", 7, 42] });
	expect(mocks.pathRefresh).toHaveBeenCalledOnce();
	expect(mocks.toast).toHaveBeenCalledExactlyOnceWith("Status changed to Confirmed");
});

it("shows a capacity error without refreshing or announcing success", async () => {
	mocks.change.mockRejectedValue(new Error("Only 0 places remaining. Choose another slot."));
	await restore();
	await expect.element(page.getByText("Only 0 places remaining. Choose another slot.")).toBeVisible();
	expect(mocks.refresh).not.toHaveBeenCalled();
	expect(mocks.toast).not.toHaveBeenCalled();
});

it("blocks another mutation after a failed refresh and allows retrying refresh alone", async () => {
	mocks.refresh.mockRejectedValueOnce(new Error("Offline"));
	await restore();
	await expect.element(page.getByRole("button", { name: "Save status" })).toBeDisabled();
	await page.getByRole("button", { name: "Cancel", exact: true }).click();
	await expect.element(page.getByRole("button", { name: "Change status", exact: true })).toBeDisabled();
	await page.getByRole("button", { name: "Refresh booking" }).click();
	await expect.element(page.getByRole("button", { name: "Change status", exact: true })).toBeEnabled();
	expect(mocks.change).toHaveBeenCalledOnce();
	expect(mocks.refresh).toHaveBeenCalledTimes(2);
	expect(mocks.toast).not.toHaveBeenCalled();
});

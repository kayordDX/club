import { beforeEach, expect, it, vi } from "vitest";
import { render } from "vitest-browser-svelte";
import { page as browserPage } from "vitest/browser";
import { BookingStatusEnum } from "$lib/api";
import NewBookingPage from "./+page.svelte";

const mocks = vi.hoisted(() => ({
	created: vi.fn(),
	invalidate: vi.fn(),
	options: [] as Array<Record<string, unknown>>,
	mutationPending: false,
	customer: { userId: "customer-user-id", firstName: "Casey", lastName: "Customer", email: "casey@example.test", phoneNumber: "0821234567" },
	contracts: [{ id: 31, slotId: "slot-1", contractId: 9, contractName: "Visitor", price: 100, validationId: null, canPayLater: true, description: "Standard" }],
	extras: [{ id: 5, name: "Cart", price: 25, description: "Cart hire" }],
}));

vi.mock("$app/state", () => ({
	page: {
		params: { slug: "club", id: "7" },
		url: new URL("http://localhost/outlet/club/7/admin/bookings/new?slotId=slot-1&date=2026-08-02&slotCount=1"),
	},
}));
vi.mock("$app/paths", () => ({
	resolve: (path: string) => path,
	assets: "",
	base: "",
	asset: (path: string) => path,
}));
vi.mock("$app/navigation", () => ({ goto: vi.fn() }));
vi.mock("$lib/auth", () => ({ useUser: () => null }));
vi.mock("svelte-sonner", () => ({ toast: { success: vi.fn(), error: vi.fn() } }));
vi.mock("@tanstack/svelte-query", () => ({
	createQuery: (factory: () => Record<string, unknown>) => {
		const options = factory();
		mocks.options.push(options);
		const key = options.queryKey as string[];
		return key[0] === "admin-booking-customer"
			? {
					get data() {
						return mocks.customer;
					},
					isFetching: false,
					isError: false,
					isPending: false,
					error: undefined,
				}
			: {
					get data() {
						return { contracts: mocks.contracts, extras: mocks.extras };
					},
					isFetching: false,
					isError: false,
					isPending: false,
					error: undefined,
				};
	},
	createMutation: () => ({ mutateAsync: mocks.created, isPending: false }),
	useQueryClient: () => ({ invalidateQueries: mocks.invalidate }),
}));
vi.mock("$lib/api/remote/admin.remote", () => ({
	adminBookingCreate: vi.fn(),
	adminBookingGetOptions: vi.fn(() => ({ refresh: vi.fn().mockResolvedValue(undefined) })),
	adminBookingSearchCustomer: vi.fn(() => ({ refresh: vi.fn().mockResolvedValue(undefined) })),
	adminSlotGetAll: vi.fn(() => ({ refresh: vi.fn().mockResolvedValue(undefined) })),
}));
vi.mock("$lib/api/remote/slot.remote", () => ({
	slotGetAll: vi.fn(() => Promise.resolve([{ id: "slot-1", total: 4, booked: 1, startDatetime: "2026-08-02T10:00:00Z", endDatetime: "2026-08-02T11:00:00Z" }])),
	slotGetContracts: vi.fn(),
}));
vi.mock("$lib/api/remote/extra.remote", () => ({ extraGetFacility: vi.fn() }));

beforeEach(() => {
	vi.clearAllMocks();
	mocks.options = [];
	mocks.customer = { userId: "customer-user-id", firstName: "Casey", lastName: "Customer", email: "casey@example.test", phoneNumber: "0821234567" };
	mocks.created.mockResolvedValue({ id: 44 });
	mocks.invalidate.mockResolvedValue(undefined);
});

it("selects the existing customer, defaults to Confirmed, and fetches owner-authorized options", async () => {
	render(NewBookingPage);
	await browserPage.getByLabelText("Search customers").fill("casey@example.test");
	await new Promise((resolve) => setTimeout(resolve, 350));
	await browserPage.getByRole("button", { name: /Casey Customer.*casey@example.test/ }).click();

	await expect.element(browserPage.getByLabelText("Name")).toHaveValue("Casey Customer");
	await expect.element(browserPage.getByLabelText("Cell No")).toHaveValue("0821234567");
	await expect.element(browserPage.getByLabelText("Email")).toHaveValue("casey@example.test");
	await expect.element(browserPage.getByRole("button", { name: "Me" })).toBeVisible();
	await browserPage.getByRole("button", { name: "Contract" }).click();
	await expect.element(browserPage.getByRole("option", { name: "Visitor Standard - R 100.00" })).toBeVisible();
	const optionsQuery = mocks.options.find((item) => (item.queryKey as string[])[0] === "admin-booking-options");
	await (optionsQuery?.queryFn as () => Promise<unknown>)();
	const { adminBookingGetOptions } = await import("$lib/api/remote/admin.remote");
	const { slotGetContracts } = await import("$lib/api/remote/slot.remote");
	expect(adminBookingGetOptions).toHaveBeenCalledWith({ facilityId: 7, params: { slotId: "slot-1", userId: "customer-user-id" } });
	expect(slotGetContracts).not.toHaveBeenCalled();
});

it("prefills player details from each selected booking owner", async () => {
	render(NewBookingPage);
	await browserPage.getByLabelText("Search customers").fill("casey@example.test");
	await new Promise((resolve) => setTimeout(resolve, 350));
	await browserPage.getByRole("button", { name: /Casey Customer.*casey@example.test/ }).click();
	await browserPage.getByLabelText("Name").fill("Draft player");

	await browserPage.getByRole("button", { name: "Choose another" }).click();
	mocks.customer = { userId: "other-customer-id", firstName: "Jordan", lastName: "Member", email: "jordan@example.test", phoneNumber: "0825550101" };
	await browserPage.getByLabelText("Search customers").fill("jordan@example.test");
	await new Promise((resolve) => setTimeout(resolve, 350));
	await browserPage.getByRole("button", { name: /Jordan Member.*jordan@example.test/ }).click();
	await expect.element(browserPage.getByLabelText("Name")).toHaveValue("Jordan Member");
	await browserPage.getByRole("button", { name: "Me" }).click();
	await expect.element(browserPage.getByLabelText("Email")).toHaveValue("jordan@example.test");
});

it("always creates Confirmed bookings and reviews the owner and total", async () => {
	render(NewBookingPage);
	await browserPage.getByLabelText("Search customers").fill("casey@example.test");
	await new Promise((resolve) => setTimeout(resolve, 350));
	await browserPage.getByRole("button", { name: /Casey Customer.*casey@example.test/ }).click();
	await browserPage.getByLabelText("Name").fill("Player One");
	await browserPage.getByLabelText("Cell No").fill("0821234567");
	await browserPage.getByLabelText("Email").fill("player@example.test");
	await browserPage.getByRole("button", { name: /Contract/ }).click();
	await browserPage.getByRole("option", { name: /Visitor Standard - R 100.00/ }).click();
	await expect.element(browserPage.getByLabelText("Booking review").getByText("Confirmed", { exact: true })).toBeVisible();
	await expect.element(browserPage.getByLabelText("Booking review").getByText("R 100,00", { exact: true })).toBeVisible();

	await browserPage.getByRole("button", { name: "Create booking" }).click();
	await expect.element(browserPage.getByText("Booking #44 created")).toBeVisible();
	const { BookingStatusEnum: Status } = await import("$lib/api");
	expect(Status.Confirmed).toBe(BookingStatusEnum.Confirmed);
	expect(mocks.created).toHaveBeenCalledWith({
		userId: "customer-user-id",
		bookingStatusId: BookingStatusEnum.Confirmed,
		bookings: [{ slotId: "slot-1", slotContractId: 31, name: "Player One", cellphone: "0821234567", email: "player@example.test" }],
		extras: [],
	});
});

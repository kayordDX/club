import { createServer } from "node:http";
import { spawn } from "node:child_process";
import { mkdtempSync, readFileSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { installErrorCapture } from "./runtime-errors.mjs";

const diagnosticDirectory = mkdtempSync(join(tmpdir(), "club-payment-errors-"));
const diagnosticPath = join(diagnosticDirectory, "errors.jsonl");
writeFileSync(diagnosticPath, "");
const recordError = installErrorCapture(diagnosticPath, "API fixture");
let diagnosticCursor = 0;
let vite;
let stopping = false;

// Test-only backend: browser requests still pass through the real authenticated SvelteKit proxy.
let scenario;
let paid;
let payments;
let grants;
let calls;
let refreshFailed;
let adminCreatedBooking;
let updateFailed;
let checkoutBookingId;
const sourceSlotId = "00000000-0000-4000-8000-000000000001";
const destinationSlotId = "00000000-0000-4000-8000-000000000002";
const slotFor = (id) => ({
	id,
	startDatetime: id === destinationSlotId ? "2026-08-03T10:00:00Z" : "2026-08-02T10:00:00Z",
	endDatetime: id === destinationSlotId ? "2026-08-03T11:00:00Z" : "2026-08-02T11:00:00Z",
	maxBookings: 4,
});
function adminSlots(date) {
	const id = date?.startsWith("2026-08-03") ? destinationSlotId : sourceSlotId;
	return [
		{
			...slotFor(id),
			resourceName: "Court A",
			total: 4,
			booked: (id === sourceSlotId ? 1 : 0) + (adminCreatedBooking?.body?.bookings[0]?.slotId === id ? adminCreatedBooking.body.bookings.length : 0),
			isEnabled: true,
			isAvailable: true,
			bookings:
				adminCreatedBooking?.body?.bookings[0]?.slotId === id
					? [
							{
								bookingId: 44,
								playerName: adminCreatedBooking.body.bookings[0].name,
								bookingStatusId: adminCreatedBooking.body.bookingStatusId,
								bookingStatusName: "Confirmed",
							},
						]
					: [],
		},
	];
}
const grant = (id, name, kind, options = {}) => ({
	grantId: `00000000-0000-4000-8000-00000000000${id}`,
	voucherId: id,
	name,
	description: "Issued independently of membership",
	isExtra: false,
	redemptionKind: kind,
	amountRemaining: 2,
	expiryDate: "2099-01-01T00:00:00Z",
	isEligible: true,
	eligibleAmount: 100,
	paymentValue: 100,
	targets: [],
	...options,
});
function reset(name) {
	scenario = name;
	paid = 0;
	payments = [];
	calls = [];
	refreshFailed = false;
	adminCreatedBooking = name.startsWith("admin-payment")
		? {
				id: 44,
				body: {
					userId: "00000000-0000-4000-8000-000000000099",
					bookingStatusId: 2,
					bookings: [{ slotId: sourceSlotId, slotContractId: 31, name: "Player One", cellphone: "0821234567", email: "player@example.test" }],
					extras: [],
				},
			}
		: null;
	updateFailed = false;
	checkoutBookingId = 123;
	grants = [
		grant(1, "Round entitlement", 1, { targets: [{ slotContractBookingId: 9, name: "Player One", unitPrice: 100, unitsAvailable: 1 }] }),
		grant(2, "Extra entitlement", 1, { isExtra: true, targets: [{ extraId: 5, name: "Cart", unitPrice: 50, unitsAvailable: 4 }] }),
		grant(3, "Capped discount", 3, { discountMode: 1, discountValue: 20, maxDiscountAmount: 40, paymentValue: 40 }),
		grant(4, "Credit voucher", 2, { amountRemaining: 60, paymentValue: 60 }),
		grant(5, "Wrong facility", 2, { isEligible: false, ineligibleReason: "This facility is not eligible." }),
	];
	if (name === "reserved") payments = [payment("Pending", 300, "payfast")];
}
function payment(status, amount, providerName) {
	return {
		id: payments.length + 1,
		transactionId: `tx-${payments.length + 1}`,
		amount,
		paymentStatus: status,
		paymentStatusId: { Pending: 1, Partial: 5, Completed: 2, Failed: 3 }[status],
		paymentTypeId: providerName === "Voucher" ? 4 : 1,
		paymentType: providerName === "Voucher" ? "Voucher" : "Card",
		providerName,
		paymentStatusDate: "2026-05-01T10:00:00Z",
	};
}
function total(bookingId) {
	if (bookingId !== 44) return 300;
	const players = adminCreatedBooking.body.bookings;
	return (
		players.length * (players[0].slotId === destinationSlotId ? 150 : 100) +
		adminCreatedBooking.body.extras.reduce((sum, extra) => sum + extra.amount * (extra.extraId === 5 ? 25 : 40), 0)
	);
}
function balance(bookingId = checkoutBookingId) {
	const attempts = bookingId === checkoutBookingId ? payments : [];
	const amountPaid = bookingId === checkoutBookingId ? paid : 0;
	const outstanding = total(bookingId) - amountPaid;
	const reserved = attempts.filter((attempt) => attempt.paymentStatusId === 1).reduce((sum, attempt) => sum + attempt.amount, 0);
	return {
		bookingId,
		amountPaid,
		amountOutstanding: outstanding,
		amountAvailable: Math.max(0, outstanding - reserved),
		isPaid: outstanding === 0,
		payments: attempts,
	};
}
reset("vouchers");
async function handleRequest(req, res) {
	const path = new URL(req.url, "http://127.0.0.1").pathname;
	let text = "";
	for await (const chunk of req) text += chunk;
	const body = text ? JSON.parse(text) : undefined;
	const send = (data, status = 200) => {
		res.writeHead(status, { "content-type": "application/json" });
		res.end(JSON.stringify(data));
	};
	if (path === "/identity/.well-known/openid-configuration") {
		return send({
			issuer: "http://127.0.0.1:5194/identity",
			authorization_endpoint: "http://127.0.0.1:5194/identity/authorize",
			token_endpoint: "http://127.0.0.1:5194/identity/token",
			jwks_uri: "http://127.0.0.1:5194/identity/jwks",
			response_types_supported: ["code"],
			subject_types_supported: ["public"],
			id_token_signing_alg_values_supported: ["RS256"],
		});
	}
	if (path === "/identity/authorize") {
		res.writeHead(200, { "content-type": "text/plain" });
		return res.end("Sign in required");
	}
	if (path === "/_test/crash-vite") {
		vite.kill("SIGKILL");
		return send({ ok: true });
	}
	if (path === "/_test/errors") {
		res.setHeader("x-payment-error-log", diagnosticPath);
		const errors = readFileSync(diagnosticPath, "utf8")
			.split("\n")
			.filter(Boolean)
			.map((line) => JSON.parse(line));
		if (req.method === "POST") {
			const unread = errors.slice(diagnosticCursor);
			diagnosticCursor = errors.length;
			return send(unread);
		}
		return send(errors);
	}
	if (path === "/_test/reset") {
		reset(body.scenario);
		return send({ ok: true });
	}
	if (path === "/_test/state") return send({ calls, ...balance() });
	if (path === "/_test/recover") {
		refreshFailed = false;
		return send({ ok: true });
	}
	if (path === "/_test/callback") {
		const current = payments.at(-1);
		current.paymentStatus = body.success ? "Partial" : "Failed";
		current.paymentStatusId = body.success ? 5 : 3;
		if (body.success) paid += current.amount;
		if (paid === total(checkoutBookingId))
			for (const p of payments)
				if (p.paymentStatus === "Partial") {
					p.paymentStatus = "Completed";
					p.paymentStatusId = 2;
				}
		return send(balance());
	}
	if (path === "/outlet") return send({ items: [], pageNumber: 1, totalPages: 0, totalCount: 0, hasPreviousPage: false, hasNextPage: false });
	calls.push({ path, query: new URL(req.url, "http://127.0.0.1").search, method: req.method, body, authorization: req.headers.authorization });
	if (!["Bearer payment-test-token", "Bearer customer-test-token", "Bearer other-customer-test-token"].includes(req.headers.authorization))
		return send({ message: "Unauthorized" }, 401);
	const manager = req.headers.authorization === "Bearer payment-test-token";
	const customer = req.headers.authorization === "Bearer customer-test-token";
	if (path.startsWith("/admin/facility/") && (!manager || !path.startsWith("/admin/facility/1/"))) return send({ message: "Forbidden" }, 403);
	if ((path === "/booking/44" || path === "/booking/44/path" || path.startsWith("/payment/booking/44")) && !customer)
		return send({ message: "Forbidden" }, 403);
	if (path === "/payment/initiate" && body.bookingId === 44 && !customer) return send({ message: "Forbidden" }, 403);
	if (path === "/admin/facility/1/booking/44/payments") return send(balance(44));
	if (path === "/admin/facility/1/booking/123/payments") return send(balance(123));
	if (path === "/payment/booking/44/vouchers") return send([]);
	if (path === "/payment/booking/44") return send(balance(44));
	if (refreshFailed && path.startsWith("/payment/booking/")) return send({}, 503);
	if (path === "/payment/booking/123/vouchers") return send(grants);
	if (path === "/payment/booking/123") return send(balance(123));
	if (path === "/booking/user")
		return send({
			items:
				adminCreatedBooking?.body?.userId === "00000000-0000-4000-8000-000000000099"
					? [
							{
								id: 44,
								bookingStatusId: adminCreatedBooking.body.bookingStatusId ?? 2,
								bookingStatusName: "Confirmed",
								bookingStatusDate: "2026-08-02T09:00:00Z",
								facilityName: "Test Course",
								slotStartDatetime: "2026-08-02T10:00:00Z",
								slotEndDatetime: "2026-08-02T11:00:00Z",
								playerCount: 1,
								amountOutstanding: 100,
								amountPaid: 0,
								isPaid: false,
								expiresAt: "2026-08-02T09:15:00Z",
							},
						]
					: [],
			pageNumber: 1,
			totalPages: 1,
			totalCount: adminCreatedBooking ? 1 : 0,
			hasPreviousPage: false,
			hasNextPage: false,
		});
	if (path === "/booking/123")
		return send({
			id: 123,
			bookingStatusId: paid === 300 ? 2 : 1,
			bookingStatus: { id: paid === 300 ? 2 : 1, name: paid === 300 ? "Confirmed" : "Pending" },
			...balance(123),
			expiresAt: new Date(Date.now() + 1800000).toISOString(),
			user: { firstName: "Payment", lastName: "Tester" },
			slotContractBookings: [
				{ id: 9, name: "Player One", slotContract: { price: 100, contractName: "Visitor", slot: { startDatetime: "2026-06-01T10:00:00Z" } } },
			],
			extraBookings: [{ id: 1, extraId: 5, amount: 4, extra: { id: 5, name: "Cart", price: 50 } }],
		});
	if (path === "/booking/123/path")
		return send({ outletSlug: "test", outletName: "Test Club", facilityId: 1, facilityName: "Test Course", slotStartDatetime: "2026-06-01T10:00:00Z" });
	if (path === "/facility/1/payment-methods")
		return send([
			{ providerName: "payfast", type: "Payfast" },
			{ providerName: "other", type: "Other provider" },
		]);
	if (path === "/outlet/basic/test") return send({ name: "Test Club", facilities: [{ id: 1, name: "Test Course" }] });
	if (path === "/account/role/1") return send(manager && scenario.startsWith("admin-") ? [{ normalizedName: "MANAGER" }] : []);
	if (path === "/slot/available") return send(true);
	if (path === "/slot" || path === "/admin/facility/1/slot") return send(adminSlots(new URL(req.url, "http://127.0.0.1").searchParams.get("date")));
	if (path === "/admin/facility/1/booking/customer/search" && new URL(req.url, "http://127.0.0.1").searchParams.get("query") !== "casey@example.test")
		return send(undefined);
	if (path === "/admin/facility/1/booking/customer/search")
		return send({
			userId: "00000000-0000-4000-8000-000000000099",
			firstName: "Casey",
			lastName: "Customer",
			email: "casey@example.test",
			phoneNumber: "0821234567",
		});
	if (path === "/admin/facility/1/booking/options") {
		const moved = new URL(req.url, "http://127.0.0.1").searchParams.get("slotId") === destinationSlotId;
		return send({
			contracts: [
				{
					id: moved ? 41 : 31,
					slotId: moved ? destinationSlotId : sourceSlotId,
					contractId: 9,
					contractName: "Visitor",
					price: moved ? 150 : 100,
					validationId: null,
					canPayLater: true,
					description: "Standard",
				},
			],
			extras: moved ? [{ id: 6, name: "Destination Cart", price: 40 }] : [{ id: 5, name: "Cart", price: 25, description: "Cart hire" }],
		});
	}
	if (path === "/admin/facility/1/booking" && req.method === "POST") {
		adminCreatedBooking = { id: 44, body };
		return send({ id: 44 });
	}
	if (path === "/admin/facility/1/booking/44" && req.method === "PUT") {
		if (scenario === "admin-move-save-error" && !updateFailed) {
			updateFailed = true;
			return send({ errors: { generalErrors: ["Destination capacity changed. Retry or choose another slot."] } }, 400);
		}
		adminCreatedBooking.body = { ...adminCreatedBooking.body, ...body };
		if (scenario === "admin-move-refresh-error") refreshFailed = true;
		return send({});
	}
	if (path === "/admin/facility/1/booking/44" || path === "/booking/44") {
		if (refreshFailed) return send({}, 503);
		const players = adminCreatedBooking.body.bookings;
		const moved = players[0].slotId === destinationSlotId;
		return send({
			id: 44,
			userId: adminCreatedBooking.body.userId,
			bookingStatusId: adminCreatedBooking.body.bookingStatusId ?? 2,
			bookingStatus: { id: adminCreatedBooking.body.bookingStatusId ?? 2, name: "Confirmed" },
			bookingStatusDate: "2026-08-02T09:00:00Z",
			expiresAt: "2026-08-02T09:15:00Z",
			...balance(44),
			user: { firstName: "Casey", lastName: "Customer" },
			slotContractBookings: players.map((player, index) => ({
				...player,
				id: index + 1,
				bookingId: 44,
				slotContract: {
					id: player.slotContractId,
					slotId: player.slotId,
					contractId: 9,
					contractName: "Visitor",
					price: moved ? 150 : 100,
					canPayLater: true,
					slot: slotFor(player.slotId),
				},
			})),
			extraBookings: adminCreatedBooking.body.extras.map((extra) => ({
				...extra,
				bookingId: 44,
				extra: { id: extra.extraId, name: extra.extraId === 5 ? "Cart" : "Destination Cart", price: extra.extraId === 5 ? 25 : 40 },
			})),
		});
	}
	if (path === "/booking/44/path" || path === "/admin/facility/1/booking/44/path")
		return send({
			outletSlug: "test",
			outletName: "Test Club",
			facilityId: 1,
			facilityName: "Test Course",
			slotId: adminCreatedBooking.body.bookings[0].slotId,
			slotStartDatetime: slotFor(adminCreatedBooking.body.bookings[0].slotId).startDatetime,
		});
	if (path === "/payment/voucher") {
		await new Promise((resolve) => setTimeout(resolve, 300));
		if (scenario === "stale" || scenario === "refresh-failure") {
			if (scenario === "refresh-failure") refreshFailed = true;
			return send({ errors: { generalErrors: ["Selected units are no longer available."] } }, 400);
		}
		const selected = grants.find((g) => g.grantId === body.grantId);
		const amount = selected.voucherId === 2 ? 50 * body.quantity : selected.paymentValue;
		paid += amount;
		payments.push(payment(paid === 300 ? "Completed" : "Partial", amount, "Voucher"));
		if (paid === 300)
			for (const p of payments)
				if (p.paymentStatus === "Partial") {
					p.paymentStatus = "Completed";
					p.paymentStatusId = 2;
				}
		selected.isEligible = false;
		selected.ineligibleReason = "Voucher already used for this booking.";
		return send({ transactionId: payments.at(-1).transactionId, amount, paymentStatusId: paid === 300 ? 2 : 5, ...balance() });
	}
	if (path === "/admin/facility/1/booking/44/payment/accept") {
		checkoutBookingId = 44;
		if (body.amount <= 0 || body.amount > balance().amountAvailable) return send({ errors: { generalErrors: ["Invalid payment amount."] } }, 400);
		paid += body.amount;
		payments.push(payment(balance().isPaid ? "Completed" : "Partial", body.amount, "Admin received payment in the club"));
		return send(balance());
	}
	if (path === "/payment/initiate" || path === "/admin/facility/1/booking/44/payment/initiate") {
		checkoutBookingId = path.startsWith("/admin/") ? 44 : body.bookingId;
		if (scenario === "provider-failure") return send({ errors: { generalErrors: ["Provider unavailable."] } }, 400);
		if (body.amount > balance().amountAvailable) return send({ errors: { generalErrors: ["Amount exceeds available balance."] } }, 400);
		payments.push(payment("Pending", body.amount, body.providerName));
		return send({
			transactionId: payments.at(-1).transactionId,
			redirectUrl: `http://127.0.0.1:5174/payment/${scenario.endsWith("-failure") ? "failure" : scenario.endsWith("-cancelled") ? "cancelled" : "success"}?transactionId=${payments.at(-1).transactionId}`,
		});
	}
	console.error("Unhandled fixture endpoint:", req.method, path);
	return send({ message: "Unknown fixture endpoint" }, 404);
}
const api = createServer((req, res) => {
	handleRequest(req, res).catch((error) => {
		recordError(`${req.method} ${req.url}: ${error.stack ?? error}`);
		res.writeHead(500);
		res.end("Unexpected API fixture error");
	});
});
api.listen(5194, "127.0.0.1", () => {
	vite = spawn(
		process.execPath,
		["--import", "./e2e/fixtures/runtime-errors.mjs", "./node_modules/vite/bin/vite.js", "dev", "--host", "127.0.0.1", "--port", "5174", "--strictPort"],
		{
			stdio: "inherit",
			env: {
				...process.env,
				PAYMENT_ERROR_LOG: diagnosticPath,
				API_URL: "http://127.0.0.1:5194",
				IDENTITY_URL: "http://127.0.0.1:5194/identity",
				APP_URL: "http://127.0.0.1:5174",
				SESSION_SECRET: "isolated-payment-playwright-secret",
				NODE_ENV: "development",
			},
		}
	);
	const stop = () => {
		stopping = true;
		vite.kill("SIGTERM");
		api.close();
	};
	process.on("SIGTERM", stop);
	process.on("SIGINT", stop);
	vite.on("error", (error) => {
		recordError(error.stack ?? error.message);
		process.exitCode = 1;
	});
	vite.on("exit", (code, signal) => {
		if (!stopping) {
			recordError(`Vite exited unexpectedly: code=${code}, signal=${signal}`);
			process.exitCode = 1;
		}
		// Keep the diagnostics API and journal available after a Vite crash.
	});
	process.on("exit", () => {
		if (stopping) rmSync(diagnosticDirectory, { recursive: true, force: true });
	});
});

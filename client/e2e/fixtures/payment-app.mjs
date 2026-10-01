import { createServer } from "node:http";
import { spawn } from "node:child_process";

// Test-only backend: browser requests still pass through the real authenticated SvelteKit proxy.
let scenario;
let paid;
let payments;
let grants;
let calls;
let refreshFailed;
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
function balance() {
	return {
		bookingId: 123,
		amountPaid: paid,
		amountOutstanding: 300 - paid,
		amountAvailable: 300 - paid,
		isPaid: paid === 300,
		payments,
	};
}
reset("vouchers");
const api = createServer(async (req, res) => {
	const path = new URL(req.url, "http://127.0.0.1").pathname;
	let text = "";
	for await (const chunk of req) text += chunk;
	const body = text ? JSON.parse(text) : undefined;
	const send = (data, status = 200) => {
		res.writeHead(status, { "content-type": "application/json" });
		res.end(JSON.stringify(data));
	};
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
		if (paid === 300)
			for (const p of payments)
				if (p.paymentStatus === "Partial") {
					p.paymentStatus = "Completed";
					p.paymentStatusId = 2;
				}
		return send(balance());
	}
	calls.push({ path, method: req.method, body, authorization: req.headers.authorization });
	if (req.headers.authorization !== "Bearer payment-test-token") return send({ message: "Unauthorized" }, 401);
	if (refreshFailed && path.startsWith("/payment/booking/")) return send({}, 503);
	if (path === "/payment/booking/123/vouchers") return send(grants);
	if (path === "/payment/booking/123") return send(balance());
	if (path === "/booking/123")
		return send({
			id: 123,
			bookingStatus: { id: paid === 300 ? 2 : 1, name: paid === 300 ? "Confirmed" : "Pending" },
			...balance(),
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
	if (path === "/account/role/1") return send([]);
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
	if (path === "/payment/initiate") {
		if (scenario === "provider-failure") return send({ errors: { generalErrors: ["Provider unavailable."] } }, 400);
		if (body.amount > balance().amountAvailable) return send({ errors: { generalErrors: ["Amount exceeds available balance."] } }, 400);
		payments.push(payment("Pending", body.amount, body.providerName));
		return send({
			transactionId: payments.at(-1).transactionId,
			redirectUrl: `http://127.0.0.1:5174/payment/success?transactionId=${payments.at(-1).transactionId}`,
		});
	}
	console.error("Unhandled fixture endpoint:", req.method, path);
	return send({ message: "Unknown fixture endpoint" }, 404);
});
api.listen(5194, "127.0.0.1", () => {
	const vite = spawn("pnpm", ["exec", "vite", "dev", "--host", "127.0.0.1", "--port", "5174", "--strictPort"], {
		stdio: "inherit",
		env: {
			...process.env,
			API_URL: "http://127.0.0.1:5194",
			APP_URL: "http://127.0.0.1:5174",
			SESSION_SECRET: "isolated-payment-playwright-secret",
			NODE_ENV: "development",
		},
	});
	const stop = () => {
		vite.kill("SIGTERM");
		api.close();
	};
	process.on("SIGTERM", stop);
	process.on("SIGINT", stop);
	vite.on("exit", () => {
		api.close();
	});
});

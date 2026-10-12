import { request } from "@playwright/test";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { paymentApiUrl } from "./payment-session";

export default async function teardown() {
	const api = await request.newContext();
	let errors: unknown[];
	try {
		const response = await api.post(`${paymentApiUrl}/_test/errors`);
		if (!response.ok()) throw new Error(`Diagnostics endpoint returned ${response.status()}`);
		errors = await response.json();
	} catch (error) {
		errors = [{ source: "suite teardown", message: String(error) }];
	} finally {
		await api.dispose();
	}
	if (errors.length) {
		const directory = process.env.PAYMENT_PROBE_OUTPUT ?? "test-results/payments";
		mkdirSync(directory, { recursive: true });
		writeFileSync(join(directory, "runtime-teardown.json"), JSON.stringify(errors, null, 2));
		throw new Error(`Unexpected runtime errors after test/context teardown: ${JSON.stringify(errors)}`);
	}
}

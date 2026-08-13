import { request } from "@playwright/test";

const FRONTEND_URL = process.env.E2E_BASE_URL ?? "http://localhost:5173";
const ATTEMPTS = 30;
const RETRY_DELAY_MS = 1000;

export default async function globalSetup() {
	const context = await request.newContext({ baseURL: FRONTEND_URL });
	try {
		let lastError: unknown;
		for (let attempt = 1; attempt <= ATTEMPTS; attempt++) {
			try {
				const response = await context.get("/");
				if (!response.ok()) {
					throw new Error(`Frontend responded with HTTP ${response.status()} at ${FRONTEND_URL}`);
				}
				await response.body();
				return;
			} catch (error) {
				lastError = error;
				await new Promise((resolve) => setTimeout(resolve, RETRY_DELAY_MS));
			}
		}
		const message = lastError instanceof Error ? lastError.message : String(lastError);
		throw new Error(
			`E2E tests need the Aspire stack running (frontend not reachable at ${FRONTEND_URL} after ${ATTEMPTS * RETRY_DELAY_MS}ms).\n` +
				`From the repo root, run: aspire start\n` +
				`Original error: ${message}`
		);
	} finally {
		await context.dispose();
	}
}

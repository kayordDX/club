import { test as base, expect } from "@playwright/test";
import { readFileSync } from "node:fs";
import { paymentApiUrl } from "./payment-session";

type RuntimeError = { source: string; message: string; time: string };

// Every isolated test uses this fixture, including newly opened tabs/popups.
export const test = base.extend<{ runtimeDiagnostics: void }>({
	runtimeDiagnostics: [
		async ({ context, request }, use, testInfo) => {
			const initial = await request.get(`${paymentApiUrl}/_test/errors`);
			const journalPath = initial.headers()["x-payment-error-log"];
			const errors: RuntimeError[] = [];
			const consoleMessages: string[] = [];
			context.on("page", (page) => {
				page.on("pageerror", (error) =>
					errors.push({ source: `browser ${page.url()}`, message: error.stack ?? error.message, time: new Date().toISOString() })
				);
				page.on("console", (message) => {
					if (["warning", "error"].includes(message.type())) {
						consoleMessages.push(`${page.url()} [${message.type()}] ${message.text()}`);
					}
				});
			});
			try {
				await use();
			} finally {
				try {
					// Keep listeners active until all pages and their context have shut down.
					await context.close();
					const response = await request.post(`${paymentApiUrl}/_test/errors`);
					expect(response.ok(), `Diagnostics endpoint returned ${response.status()}`).toBe(true);
					errors.push(...(await response.json()));
				} catch (error) {
					errors.push({ source: "diagnostic collection", message: String(error), time: new Date().toISOString() });
					if (journalPath) {
						try {
							errors.push(
								...readFileSync(journalPath, "utf8")
									.split("\n")
									.filter(Boolean)
									.map((line) => JSON.parse(line))
							);
						} catch (journalError) {
							errors.push({ source: "server journal", message: String(journalError), time: new Date().toISOString() });
						}
					}
				}
				if (errors.length || testInfo.status !== testInfo.expectedStatus) {
					await testInfo.attach("runtime-diagnostics", {
						body: JSON.stringify({ test: testInfo.titlePath, errors, consoleMessages }, null, 2),
						contentType: "application/json",
					});
				}
				expect(errors, "Unexpected unhandled browser or fixture/server errors (see runtime-diagnostics attachment)").toEqual([]);
			}
		},
		{ auto: true },
	],
});

export { expect };

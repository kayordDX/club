import { test as base, expect } from "@playwright/test";
import { paymentApiUrl } from "./payment-session";

type RuntimeError = { source: string; message: string; time: string };

// Every isolated test uses this fixture, including newly opened tabs/popups.
export const test = base.extend<{ runtimeDiagnostics: void }>({
	runtimeDiagnostics: [
		async ({ context, request }, use, testInfo) => {
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
				const response = await request.post(`${paymentApiUrl}/_test/errors`);
				expect(response.ok(), "Runtime diagnostics endpoint must remain available").toBe(true);
				const serverErrors: RuntimeError[] = await response.json();
				errors.push(...serverErrors);
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

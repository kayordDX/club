import { expect, it } from "vitest";
import { apiErrorMessage } from "./error-message";

it("preserves backend validation and stale selection reasons", () => {
	expect(apiErrorMessage(400, { errors: { generalErrors: ["Selected units are no longer available."] } })).toBe("Selected units are no longer available.");
});
it("does not expose internal errors or assume JSON error responses", () => {
	for (const body of [undefined, "bad gateway", { reason: "database secret" }, { errors: { generalErrors: ["internal exception"] } }]) {
		expect(apiErrorMessage(500, body)).toBe("API request failed: 500");
	}
	expect(apiErrorMessage(400, { errors: null })).toBe("API request failed: 400");
});

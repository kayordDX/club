export function apiErrorMessage(status: number, body: unknown): string {
	if (status < 500 && body && typeof body === "object" && "errors" in body && body.errors && typeof body.errors === "object") {
		const messages = Object.values(body.errors)
			.flat()
			.filter((value): value is string => typeof value === "string");
		if (messages.length) return messages.join(" ");
	}
	return `API request failed: ${status}`;
}

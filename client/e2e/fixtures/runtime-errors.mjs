import { appendFileSync } from "node:fs";
import { format, stripVTControlCharacters } from "node:util";

export function installErrorCapture(path, source) {
	const record = (message) => {
		appendFileSync(path, JSON.stringify({ source, message, time: new Date().toISOString() }) + "\n");
	};
	const original = console.error;
	console.error = (...args) => {
		const message = format(...args);
		// Kit 3 logs denied SSR requests even when the application handles them.
		// Keep these on stderr, but do not classify bare HTTP 403 summaries as crashes.
		if (!(source === "SvelteKit/Vite" && /^403 (GET|HEAD) \/[^\n]*$/.test(stripVTControlCharacters(message).trim()))) record(message);
		original(...args);
	};
	process.on("uncaughtExceptionMonitor", (error) => record(error.stack ?? error.message));
	return record;
}

if (process.env.PAYMENT_ERROR_LOG) {
	installErrorCapture(process.env.PAYMENT_ERROR_LOG, "SvelteKit/Vite");
}

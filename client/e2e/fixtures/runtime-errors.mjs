import { appendFileSync } from "node:fs";
import { format } from "node:util";

export function installErrorCapture(path, source) {
	const record = (message) => {
		appendFileSync(path, JSON.stringify({ source, message, time: new Date().toISOString() }) + "\n");
	};
	const original = console.error;
	console.error = (...args) => {
		record(format(...args));
		original(...args);
	};
	process.on("uncaughtExceptionMonitor", (error) => record(error.stack ?? error.message));
	return record;
}

if (process.env.PAYMENT_ERROR_LOG) {
	installErrorCapture(process.env.PAYMENT_ERROR_LOG, "SvelteKit/Vite");
}

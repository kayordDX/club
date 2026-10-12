import { test } from "node:test";
import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { cpSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";

test("runtime diagnostics fail otherwise-passing browser/fixture probes, not handled errors", { timeout: 180000 }, () => {
	const directory = mkdtempSync(join(tmpdir(), "club-runtime-probes-"));
	try {
		const report = join(directory, "report.json");
		const result = spawnSync(process.execPath, ["node_modules/@playwright/test/cli.js", "test", "--config", "e2e/fixtures/runtime-errors.config.ts"], {
			encoding: "utf8",
			timeout: 150000,
			env: { ...process.env, PLAYWRIGHT_JSON_OUTPUT_FILE: report, PAYMENT_PROBE_OUTPUT: join(directory, "artifacts") },
		});
		writeFileSync(join(directory, "runner-stdout.log"), result.stdout ?? "");
		writeFileSync(join(directory, "runner-stderr.log"), result.stderr ?? "");
		assert.equal(result.status, 1, result.stderr);
		const json = JSON.parse(readFileSync(report, "utf8"));
		assert.equal(json.errors.length, 0, JSON.stringify(json.errors));
		const specs = json.suites.flatMap((suite) => suite.specs);
		assert.equal(specs.length, 7);
		const markers = ["probe-browser-exception", "probe-browser-rejection", "probe-extra-page", "API fixture", "probe-context-close"];
		for (const [index, spec] of specs.entries()) {
			const execution = spec.tests[0].results[0];
			assert.ok(
				execution.attachments.some((attachment) => attachment.name === "probe-body-completed"),
				spec.title
			);
			if (index < markers.length) {
				assert.equal(execution.status, "failed", spec.title);
				assert.match(execution.errors.map((error) => error.message).join("\n"), /Unexpected unhandled browser or fixture\/server errors/);
				const diagnostics = execution.attachments.find((attachment) => attachment.name === "runtime-diagnostics");
				assert.ok(diagnostics, spec.title);
				const body = diagnostics.body ? Buffer.from(diagnostics.body, "base64").toString() : readFileSync(diagnostics.path, "utf8");
				assert.ok(body.includes(markers[index]), body);
			} else if (index === markers.length) {
				assert.equal(execution.status, "passed", spec.title);
			} else {
				assert.equal(execution.status, "failed", spec.title);
				const diagnostics = execution.attachments.find((attachment) => attachment.name === "runtime-diagnostics");
				assert.ok(diagnostics, spec.title);
				const body = diagnostics.body ? Buffer.from(diagnostics.body, "base64").toString() : readFileSync(diagnostics.path, "utf8");
				assert.match(body, /Vite exited unexpectedly/);
			}
		}
	} catch (error) {
		const retained = resolve("test-results/diagnostics");
		mkdirSync(retained, { recursive: true });
		cpSync(directory, retained, { recursive: true });
		writeFileSync(join(retained, "verification-error.txt"), String(error));
		throw error;
	} finally {
		rmSync(directory, { recursive: true, force: true });
	}
});

test("server preload captures caught errors and uncaught exceptions without hiding exit failure", () => {
	const directory = mkdtempSync(join(tmpdir(), "club-server-probe-"));
	try {
		const log = join(directory, "errors.jsonl");
		const result = spawnSync(
			process.execPath,
			[
				"--import",
				resolve("e2e/fixtures/runtime-errors.mjs"),
				"--input-type=module",
				"-e",
				'console.warn("allowed warning"); console.error(new Error("probe-caught-server")); throw new Error("probe-uncaught-server");',
			],
			{ encoding: "utf8", env: { ...process.env, PAYMENT_ERROR_LOG: log } }
		);
		assert.equal(result.status, 1);
		const errors = readFileSync(log, "utf8")
			.trim()
			.split("\n")
			.map((line) => JSON.parse(line));
		assert.equal(errors.length, 2);
		assert.ok(errors.every((error) => error.source === "SvelteKit/Vite"));
		assert.match(errors[0].message, /probe-caught-server/);
		assert.match(errors[1].message, /probe-uncaught-server/);
	} finally {
		rmSync(directory, { recursive: true, force: true });
	}
});

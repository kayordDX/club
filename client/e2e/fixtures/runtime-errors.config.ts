import { defineConfig } from "@playwright/test";
import payments from "../../playwright.payments.config";

export default defineConfig({
	...payments,
	testDir: ".",
	testMatch: "runtime-errors.probe.ts",
	reporter: "json",
	outputDir: process.env.PAYMENT_PROBE_OUTPUT,
});

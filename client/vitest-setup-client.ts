/// <reference types="@vitest/browser/matchers" />
/// <reference types="@vitest/browser/providers/playwright" />

import { afterEach, beforeEach, expect, vi } from "vitest";

let warn: ReturnType<typeof vi.spyOn>;
beforeEach(() => {
	warn = vi.spyOn(console, "warn");
});
afterEach(() => {
	const proxyWarnings = warn.mock.calls.filter((args) => args.some((arg) => String(arg).includes("state_proxy_equality_mismatch")));
	warn.mockRestore();
	expect(proxyWarnings, "Browser interactions must not emit Svelte proxy-identity warnings").toEqual([]);
});

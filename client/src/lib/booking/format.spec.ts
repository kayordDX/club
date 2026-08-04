import { describe, expect, it } from "vitest";

import { formatCurrency, formatDate, formatDateTime, formatTime } from "./format";

describe("booking formatting", () => {
	it("formats currency in en-ZA ZAR", () => {
		expect(formatCurrency(1234.5)).toContain("234,50");
		expect(formatCurrency(0)).toContain("0,00");
	});

	it("returns a dash for missing values", () => {
		expect(formatCurrency(null)).toBe("—");
		expect(formatCurrency(undefined)).toBe("—");
		expect(formatDate(null)).toBe("—");
		expect(formatDate(undefined)).toBe("—");
		expect(formatTime("")).toBe("—");
		expect(formatDateTime(undefined)).toBe("—");
	});

	it("formats dates and times", () => {
		// Noon UTC keeps the date stable in UTC-based test environments.
		expect(formatDate("2026-08-01T12:00:00Z")).toContain("Aug 2026");
		expect(formatTime("2026-08-01T12:00:00Z")).toBe("12:00");
		expect(formatDateTime("2026-08-01T12:00:00Z")).toContain("2026");
	});
});

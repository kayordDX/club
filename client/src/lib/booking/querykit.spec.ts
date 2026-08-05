import { describe, expect, it } from "vitest";

import { sortingToQueryKitSorts, buildBookingFilters } from "./querykit";
import type { SortingState } from "@tanstack/table-core";

describe("sortingToQueryKitSorts", () => {
	it("returns undefined for an empty sorting state", () => {
		expect(sortingToQueryKitSorts([])).toBeUndefined();
	});

	it("encodes a single ascending sort without a prefix", () => {
		expect(sortingToQueryKitSorts([{ id: "slotStartDatetime", desc: false }])).toBe("slotStartDatetime");
	});

	it("prefixes descending sorts with a dash", () => {
		expect(sortingToQueryKitSorts([{ id: "slotStartDatetime", desc: true }])).toBe("-slotStartDatetime");
	});

	it("joins multiple sorts with commas", () => {
		const sorting: SortingState = [
			{ id: "slotStartDatetime", desc: true },
			{ id: "id", desc: false },
		];
		expect(sortingToQueryKitSorts(sorting)).toBe("-slotStartDatetime,id");
	});
});

describe("buildBookingFilters", () => {
	it("returns undefined when nothing is supplied", () => {
		expect(buildBookingFilters({})).toBeUndefined();
	});

	it("builds a status filter", () => {
		expect(buildBookingFilters({ status: 4 })).toBe("bookingStatusId == 4");
	});

	it("builds an exact booking id filter for a whole number", () => {
		expect(buildBookingFilters({ search: "123" })).toBe("id == 123");
	});

	it("ignores non-numeric search text", () => {
		expect(buildBookingFilters({ search: "abc" })).toBeUndefined();
	});

	it("ignores partial numeric search text", () => {
		expect(buildBookingFilters({ search: "12a" })).toBeUndefined();
	});

	it("combines status and id with AND", () => {
		expect(buildBookingFilters({ status: 2, search: "5" })).toBe("bookingStatusId == 2 && id == 5");
	});
});

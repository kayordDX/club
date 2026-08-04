import { describe, expect, it } from "vitest";

import { getSelectedExtrasTotal } from "./pricing";

describe("getSelectedExtrasTotal", () => {
	it("adds every selected extra amount into the total", () => {
		expect(
			getSelectedExtrasTotal([
				{ id: 1, name: "Golf Cart", price: 300, amount: 2 },
				{ id: 2, name: "Caddie", price: 150, amount: 1 },
			])
		).toBe(750);
	});
});

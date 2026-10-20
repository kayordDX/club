import { describe, expect, it } from "vitest";
import { getPlayersSchema } from "./schema";

const player = { name: "Guest", contractId: "31", cellNo: "", email: "" };

describe("guest player contact validation", () => {
	it("allows name-only guests without relaxing account booking validation", () => {
		expect(getPlayersSchema(true).safeParse({ players: [player] }).success).toBe(true);
		expect(getPlayersSchema(false).safeParse({ players: [player] }).success).toBe(false);
	});

	it("still requires names and contracts and validates supplied guest contact details", () => {
		for (const fields of [{ name: "" }, { contractId: "" }, { cellNo: "abc" }, { email: "invalid" }]) {
			expect(getPlayersSchema(true).safeParse({ players: [{ ...player, ...fields }] }).success).toBe(false);
		}
		expect(getPlayersSchema(true).safeParse({ players: [] }).success).toBe(false);
		expect(getPlayersSchema(true).safeParse({ players: [{ ...player, cellNo: "0821234567", email: "guest@example.com" }] }).success).toBe(true);
	});
});

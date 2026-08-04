import { describe, expect, it } from "vitest";

import { applyFirstPlayerContract, applyProfileToPlayer, createPlayers } from "./players";

describe("createPlayers", () => {
	it("creates the requested number of empty players", () => {
		expect(createPlayers(3)).toEqual([
			{ name: "", cellNo: "", email: "", contractId: "" },
			{ name: "", cellNo: "", email: "", contractId: "" },
			{ name: "", cellNo: "", email: "", contractId: "" },
		]);
	});
});

describe("applyFirstPlayerContract", () => {
	it("propagates the first player's contract to players without a contract", () => {
		const players = [
			{ name: "A", cellNo: "1", email: "a@x.com", contractId: "10" },
			{ name: "B", cellNo: "2", email: "b@x.com", contractId: "" },
			{ name: "C", cellNo: "3", email: "c@x.com", contractId: "" },
		];

		expect(applyFirstPlayerContract(players)).toEqual([
			{ name: "A", cellNo: "1", email: "a@x.com", contractId: "10" },
			{ name: "B", cellNo: "2", email: "b@x.com", contractId: "10" },
			{ name: "C", cellNo: "3", email: "c@x.com", contractId: "10" },
		]);
	});

	it("does not override contracts that are already set", () => {
		const players = [
			{ name: "A", cellNo: "1", email: "a@x.com", contractId: "10" },
			{ name: "B", cellNo: "2", email: "b@x.com", contractId: "20" },
			{ name: "C", cellNo: "3", email: "c@x.com", contractId: "" },
		];

		expect(applyFirstPlayerContract(players)).toEqual([
			{ name: "A", cellNo: "1", email: "a@x.com", contractId: "10" },
			{ name: "B", cellNo: "2", email: "b@x.com", contractId: "20" },
			{ name: "C", cellNo: "3", email: "c@x.com", contractId: "10" },
		]);
	});

	it("does nothing when the first player has no contract", () => {
		const players = [
			{ name: "A", cellNo: "1", email: "a@x.com", contractId: "" },
			{ name: "B", cellNo: "2", email: "b@x.com", contractId: "" },
		];

		expect(applyFirstPlayerContract(players)).toEqual(players);
	});

	it("does nothing for a single player", () => {
		const players = [{ name: "A", cellNo: "1", email: "a@x.com", contractId: "10" }];

		expect(applyFirstPlayerContract(players)).toEqual(players);
	});
});

describe("applyProfileToPlayer", () => {
	it("fills player details from the profile while keeping the contract", () => {
		const player = { name: "", cellNo: "", email: "", contractId: "10" };

		expect(applyProfileToPlayer(player, { name: "Jaco", email: "jaco@example.com", phone_number: "0842502311" })).toEqual({
			name: "Jaco",
			cellNo: "0842502311",
			email: "jaco@example.com",
			contractId: "10",
		});
	});

	it("clears fields missing from the profile", () => {
		const player = { name: "Old", cellNo: "082", email: "old@example.com", contractId: "10" };

		expect(applyProfileToPlayer(player, {})).toEqual({ name: "", cellNo: "", email: "", contractId: "10" });
	});
});

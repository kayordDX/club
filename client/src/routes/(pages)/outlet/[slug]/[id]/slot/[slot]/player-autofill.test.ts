import { describe, expect, it } from "vitest";
import {
	SELF_PLAYER_OPTION_VALUE,
	getSelfPlayerDetails,
	getSelfPlayerOption,
} from "./player-autofill";

describe("player-autofill", () => {
	it("returns the me option when the logged-in user has details", () => {
		expect(
			getSelfPlayerOption({
				name: "Jamie Tester",
				email: "jamie@example.com",
			})
		).toEqual({
			value: SELF_PLAYER_OPTION_VALUE,
			label: "Me",
		});
	});

	it("maps profile details to booking player fields", () => {
		expect(
			getSelfPlayerDetails({
				name: "Jamie Tester",
				email: "jamie@example.com",
				phone_number: "082 123 4567",
			})
		).toEqual({
			name: "Jamie Tester",
			email: "jamie@example.com",
			cellNo: "082 123 4567",
		});
	});

	it("falls back to first and last name when full name is missing", () => {
		expect(
			getSelfPlayerDetails({
				given_name: "Jamie",
				family_name: "Tester",
				email: "jamie@example.com",
			})
		).toEqual({
			name: "Jamie Tester",
			email: "jamie@example.com",
			cellNo: "",
		});
	});

	it("ignores empty profiles", () => {
		expect(
			getSelfPlayerOption({
				name: " ",
				email: " ",
				phone_number: " ",
			})
		).toBeNull();
		expect(getSelfPlayerDetails()).toBeNull();
	});
});

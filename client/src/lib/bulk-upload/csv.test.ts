import { describe, expect, it } from "vitest";
import { parseMembershipCsv } from "./csv";

const header = "First Name,Last Name,Mobile,Email,ContractId";

describe("membership CSV parser", () => {
	it("reads quoted fields and escaped quotes", () => {
		expect(parseMembershipCsv(`\uFEFF${header}\r\n"Anne, M.","O""Brien",0123,a@example.com,42\r\n`)).toEqual([
			{ line: 2, firstName: "Anne, M.", lastName: 'O"Brien', mobile: "0123", email: "a@example.com", contractId: 42 },
		]);
	});

	it("rejects a wrong header, malformed quoting and wrong column counts", () => {
		expect(() => parseMembershipCsv("Email,Contract")).toThrow("Expected columns");
		expect(() => parseMembershipCsv(`${header}\n"Anne,Smith,0123,a@example.com,Gold`)).toThrow("Unclosed");
		expect(() => parseMembershipCsv(`${header}\nAnne,Smith`)).toThrow("five columns");
		expect(() => parseMembershipCsv(`${header}\nAnne,Smith,0123,a@example.com,Gold`)).toThrow("Invalid contract ID");
	});
});

import { beforeEach, describe, expect, it, vi } from "vitest";

const fetchMock = vi.fn();

vi.stubGlobal("fetch", fetchMock);
vi.mock("$app/server", () => ({ getRequestEvent: () => ({ locals: { accessToken: "tok" } }) }));
vi.mock("$app/env/private", () => ({ API_URL: "http://api.test" }));

const { default: customServerInstance } = await import("./client");

const jsonResponse = (body: string, status = 200) => new Response(body, { status, headers: { "content-type": "application/json" } });

describe("customServerInstance", () => {
	beforeEach(() => {
		fetchMock.mockReset();
	});

	it("parses a json body", async () => {
		fetchMock.mockResolvedValue(jsonResponse(JSON.stringify({ id: 1 })));
		await expect(customServerInstance("/x")).resolves.toEqual({ id: 1 });
	});

	it("returns undefined for an empty 200 body (member search 'not found')", async () => {
		fetchMock.mockResolvedValue(jsonResponse(""));
		await expect(customServerInstance("/x")).resolves.toBeUndefined();
	});

	it("returns undefined for a 204 response", async () => {
		fetchMock.mockResolvedValue(new Response(null, { status: 204 }));
		await expect(customServerInstance("/x")).resolves.toBeUndefined();
	});

	it("throws a 404 kit error when the api responds 404", async () => {
		fetchMock.mockResolvedValue(new Response(null, { status: 404 }));
		await expect(customServerInstance("/x")).rejects.toMatchObject({ status: 404 });
	});
});

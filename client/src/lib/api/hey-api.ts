import type { CreateClientConfig } from "./generated/client/types.gen";

export const createClientConfig: CreateClientConfig = (config) => ({
	...config,
	baseUrl: "https://example.com",
});

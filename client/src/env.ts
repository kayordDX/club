import { defineEnvVars } from "@sveltejs/kit/env";
import z from "zod";

export const variables = defineEnvVars({
	API_URL: {
		default: "http://localhost:5000",
		description: "The backend url",
		schema: z.string(),
	},
	APP_URL: {
		description: "This front end url",
		schema: z.string(),
	},
	IDENTITY_URL: {
		description: "The auth service or keycloak url",
		schema: z.string(),
	},
	SESSION_SECRET: {
		description: "Secret used to encrypt the cookies",
		schema: z.string().min(32, { error: "must be at least 32 characters long" }),
	},
});

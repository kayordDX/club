import prettier from "eslint-config-prettier";
import { includeIgnoreFile } from "@eslint/config-helpers";
import js from "@eslint/js";
import ts from "typescript-eslint";
import svelte from "eslint-plugin-svelte";
import globals from "globals";
import { fileURLToPath } from "node:url";
import { defineConfig } from "eslint/config";
import svelteConfig from "./svelte.config.js";

const gitignorePath = fileURLToPath(new URL("./.gitignore", import.meta.url));

export default defineConfig(
	includeIgnoreFile(gitignorePath),
	js.configs.recommended,
	...svelte.configs.recommended,
	prettier,
	...svelte.configs.prettier,
	{
		languageOptions: {
			globals: { ...globals.browser, ...globals.node },
		},
		rules: {
			// typescript-eslint strongly recommend that you do not use the no-undef lint rule on TypeScript projects.
			// see: https://typescript-eslint.io/troubleshooting/faqs/eslint/#i-get-errors-from-the-no-undef-rule-about-global-variables-not-being-defined-even-though-there-are-no-typescript-errors
			"no-undef": "off",
			// Allow underscore-prefixed unused params (e.g. `_error` in <svelte:boundary> failed snippets).
			"no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_" }],
		},
	},
	// Plain TypeScript files: use the typescript-eslint parser and TS-aware rules.
	// The base `no-unused-vars` rule produces false positives on TS constructs
	// (ambient declarations, enum members, function-type params), so it is replaced.
	{
		files: ["**/*.ts", "**/*.tsx"],
		languageOptions: {
			parser: ts.parser,
		},
		plugins: {
			"@typescript-eslint": ts.plugin,
		},
		rules: {
			"no-unused-vars": "off",
			"@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_" }],
			"@typescript-eslint/no-explicit-any": "error",
		},
	},
	{
		files: ["**/*.svelte", "**/*.svelte.ts", "**/*.svelte.js"],
		languageOptions: {
			parserOptions: {
				projectService: true,
				extraFileExtensions: [".svelte"],
				parser: ts.parser,
				svelteConfig,
			},
		},
	},
	// Machine-generated API client output (orval + tools/gen-remote.mjs) — not linted.
	{
		ignores: ["src/lib/api/generated/**", "src/lib/api/remote/**", "src/lib/server/api/generated/**"],
	}
);

// Generates SvelteKit remote-function wrappers (.remote.ts) from swagger.json.
//
// For each operation it emits a `query` (GET) or `command` (other) that wraps the
// orval-generated server transport in src/lib/server/api/generated/<tag>.ts.
//
// Run: `node tools/gen-remote.mjs`  (also wired into `pnpm api`)
//
// Conventions encoded here:
//  - Remote functions take ONE argument, so path/query/body are bundled into one
//    object: path params by name, query params under `params`, body under `body`.
//  - Path params get real Zod validation; body & query params are type-checked via
//    z.custom<T>() (the .NET API / FastEndpoints remains the validation source of
//    truth for complex payloads). Swap to real Zod by referencing orval's `zod`
//    client output if you want deep client-side validation.
//  - Operations with no required inputs use the no-arg overload.
import { readFileSync, writeFileSync, rmSync, mkdirSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const CLIENT = resolve(__dirname, "..", "client");
const SPEC_PATH = resolve(CLIENT, "swagger.json");
const OUT_DIR = resolve(CLIENT, "src/lib/api/remote");
const TRANSPORT_BASE = "$lib/server/api/generated";

const spec = JSON.parse(readFileSync(SPEC_PATH, "utf8"));

const zodForType = (type) => {
	switch (type) {
		case "integer":
			return "z.number().int()";
		case "number":
			return "z.number()";
		case "boolean":
			return "z.boolean()";
		case "string":
			return "z.string()";
		default:
			return "z.unknown()";
	}
};

const camel = (opId) => opId[0].toLowerCase() + opId.slice(1);
const nameFromRef = (ref) => ref.split("/").pop();

/** @type {Map<string, { imports: Set<string>, lines: string[] }>} */
const byTag = new Map();

for (const [path, methods] of Object.entries(spec.paths ?? {})) {
	for (const [method, op] of Object.entries(methods)) {
		if (!op.operationId) continue;

		const opId = op.operationId;
		const fnName = camel(opId);
		const kind = method.toUpperCase() === "GET" ? "query" : "command";
		const tag = (op.tags?.[0] ?? "misc").toLowerCase();

		const params = op.parameters ?? [];
		const pathParams = params.filter((p) => p.in === "path");
		const queryParams = params.filter((p) => p.in === "query");
		const requiredQuery = queryParams.filter((p) => p.required);
		const bodyRef = op.requestBody?.content?.["application/json"]?.schema?.$ref;
		const bodyType = bodyRef ? nameFromRef(bodyRef) : null;

		const imports = new Set();

		// No required input (only optional query, or nothing) → no-arg overload.
		const hasMeaningful = pathParams.length > 0 || !!bodyType || requiredQuery.length > 0;

		let line;
		if (!hasMeaningful) {
			line = `export const ${fnName} = ${kind}(async () => api.${fnName}());`;
		} else {
			// A "slot" is one positional argument to the orval transport.
			// Each: { arg, schema } where `arg` is the JS binding name.
			const slots = [];
			for (const p of pathParams) {
				slots.push({ arg: p.name, schema: zodForType(p.schema?.type) });
			}
			if (queryParams.length) {
				const paramsType = `${opId}Params`;
				imports.add(paramsType);
				const optional = requiredQuery.length === 0 ? ".optional()" : "";
				slots.push({ arg: "params", schema: `z.custom<${paramsType}>()${optional}` });
			}
			if (bodyType) {
				imports.add(bodyType);
				slots.push({ arg: "body", schema: `z.custom<${bodyType}>()` });
			}

			if (slots.length === 1) {
				// Single input → bare schema (idiomatic: getPost(slug)).
				const s = slots[0];
				line = `export const ${fnName} = ${kind}(${s.schema}, async (${s.arg}) => api.${fnName}(${s.arg}));`;
			} else {
				// Multiple inputs → one object argument keyed by name.
				const entries = slots.map((s) => `${s.arg}: ${s.schema}`);
				const keys = slots.map((s) => s.arg);
				line = `export const ${fnName} = ${kind}(z.object({ ${entries.join(", ")} }), async ({ ${keys.join(", ")} }) => api.${fnName}(${keys.join(", ")}));`;
			}
		}

		if (!byTag.has(tag)) byTag.set(tag, { imports: new Set(), lines: [] });
		const bucket = byTag.get(tag);
		for (const t of imports) bucket.imports.add(t);
		bucket.lines.push(line);
	}
}

// Fresh output dir (generated files only — hand-written .remote.ts live elsewhere).
rmSync(OUT_DIR, { recursive: true, force: true });
mkdirSync(OUT_DIR, { recursive: true });

const header =
	"// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.\n" +
	"// Remote functions (query/command) wrapping the orval-generated server transport.\n";

let total = 0;
for (const [tag, { imports, lines }] of byTag) {
	const types = [...imports].sort();
	const typeImport = types.length
		? `import type { ${types.join(", ")} } from "${TRANSPORT_BASE}/server.schemas";\n`
		: "";
	const content =
		`${header}\n` +
		`import { query, command } from "$app/server";\n` +
		`import { z } from "zod";\n` +
		`import * as api from "${TRANSPORT_BASE}/${tag}";\n` +
		typeImport +
		"\n" +
		lines.join("\n") +
		"\n";
	writeFileSync(resolve(OUT_DIR, `${tag}.remote.ts`), content);
	total += lines.length;
	console.log(`  ✓ ${tag}.remote.ts (${lines.length} functions)`);
}

console.log(`\nGenerated ${total} remote functions across ${byTag.size} files → src/lib/api/remote/`);

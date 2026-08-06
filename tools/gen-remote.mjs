// Generates SvelteKit remote-function wrappers (.remote.ts) from swagger.json.
//
// For each operation it emits a `query` (GET) or `command` (other) that wraps the
// orval-generated server transport in src/lib/server/api/generated/<tag>.ts.
//
// Validation schemas come from orval's `zod` client output
// (src/lib/server/api/schemas/<tag>.ts): {OpId}QueryParams, {OpId}Body.
// Path params use inline primitive Zod to keep bare single-value ergonomics.
//
// Run: `node tools/gen-remote.mjs`  (also wired into `pnpm api`)
import { readFileSync, writeFileSync, rmSync, mkdirSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const CLIENT = resolve(__dirname, "..", "client");
const SPEC_PATH = resolve(CLIENT, "swagger.json");
const OUT_DIR = resolve(CLIENT, "src/lib/api/remote");
const TRANSPORT_BASE = "$lib/server/api/generated";
const SCHEMAS_BASE = "$lib/server/api/schemas";

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

/** @type {Map<string, { schemaImports: Set<string>, lines: string[] }>} */
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
		const hasBody = !!op.requestBody?.content?.["application/json"]?.schema?.$ref;

		const schemaImports = new Set();

		// A "slot" is one positional argument to the orval transport.
		const slots = [];
		for (const p of pathParams) {
			slots.push({ arg: p.name, schema: zodForType(p.schema?.type) });
		}
		if (queryParams.length) {
			// ALL query params are exposed via the generated {OpId}QueryParams schema.
			const qSchema = `${opId}QueryParams`;
			schemaImports.add(qSchema);
			const optional = requiredQuery.length === 0 ? ".optional()" : "";
			slots.push({ arg: "params", schema: `${qSchema}${optional}` });
		}
		if (hasBody) {
			const bSchema = `${opId}Body`;
			schemaImports.add(bSchema);
			slots.push({ arg: "body", schema: bSchema });
		}

		let line;
		if (slots.length === 0) {
			// Truly input-less operation → no-arg overload.
			line = `export const ${fnName} = ${kind}(async () => api.${fnName}());`;
		} else if (slots.length === 1) {
			// Single input → bare schema (idiomatic: getPost(slug)).
			const s = slots[0];
			line = `export const ${fnName} = ${kind}(${s.schema}, async (${s.arg}) => api.${fnName}(${s.arg}));`;
		} else {
			// Multiple inputs → one object argument keyed by name.
			const entries = slots.map((s) => `${s.arg}: ${s.schema}`);
			const keys = slots.map((s) => s.arg);
			line = `export const ${fnName} = ${kind}(z.object({ ${entries.join(", ")} }), async ({ ${keys.join(", ")} }) => api.${fnName}(${keys.join(", ")}));`;
		}

		if (!byTag.has(tag)) byTag.set(tag, { schemaImports: new Set(), lines: [] });
		const bucket = byTag.get(tag);
		for (const s of schemaImports) bucket.schemaImports.add(s);
		bucket.lines.push(line);
	}
}

// Fresh output dir (generated files only — hand-written .remote.ts live elsewhere).
rmSync(OUT_DIR, { recursive: true, force: true });
mkdirSync(OUT_DIR, { recursive: true });

const header =
	"// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.\n" +
	"// Remote functions (query/command) wrapping the orval-generated server transport.\n" +
	"// Validation schemas come from orval's zod client output.\n";

let total = 0;
for (const [tag, { schemaImports, lines }] of byTag) {
	const schemas = [...schemaImports].sort();
	const schemaImport = schemas.length
		? `import { ${schemas.join(", ")} } from "${SCHEMAS_BASE}/${tag}";\n`
		: "";
	const content =
		`${header}\n` +
		`import { query, command } from "$app/server";\n` +
		`import { z } from "zod";\n` +
		`import * as api from "${TRANSPORT_BASE}/${tag}";\n` +
		schemaImport +
		"\n" +
		lines.join("\n") +
		"\n";
	writeFileSync(resolve(OUT_DIR, `${tag}.remote.ts`), content);
	total += lines.length;
	console.log(`  \u2713 ${tag}.remote.ts (${lines.length} functions)`);
}

console.log(`\nGenerated ${total} remote functions across ${byTag.size} files → src/lib/api/remote/`);

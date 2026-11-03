export interface MembershipRow {
	line: number;
	firstName: string;
	lastName: string;
	mobile: string;
	email: string;
	contractId: number;
}

const headers = ["first name", "last name", "mobile", "email", "contractid"];

/** Parse the membership export without persisting its contents. */
export function parseMembershipCsv(text: string): MembershipRow[] {
	const records: { line: number; fields: string[] }[] = [];
	let fields: string[] = [];
	let field = "";
	let line = 1;
	let recordLine = 1;
	let quoted = false;
	let closed = false;
	text = text.replace(/^\uFEFF/, "");

	for (let i = 0; i < text.length; i++) {
		const char = text[i];
		if (quoted) {
			if (char === '"') {
				if (text[i + 1] === '"') {
					field += '"';
					i++;
				} else {
					quoted = false;
					closed = true;
				}
			} else {
				field += char;
				if (char === "\n") line++;
			}
		} else if (char === ",") {
			fields.push(field);
			field = "";
			closed = false;
		} else if (char === "\r" || char === "\n") {
			if (char === "\r" && text[i + 1] === "\n") i++;
			fields.push(field);
			records.push({ line: recordLine, fields });
			fields = [];
			field = "";
			closed = false;
			recordLine = ++line;
		} else if (char === '"' && field === "" && !closed) {
			quoted = true;
		} else {
			if (closed || char === '"') throw new Error(`Invalid CSV quoting on line ${line}.`);
			field += char;
		}
	}
	if (quoted) throw new Error(`Unclosed quoted field on line ${recordLine}.`);
	if (field !== "" || fields.length || closed) records.push({ line: recordLine, fields: [...fields, field] });
	const header = records.shift()?.fields.map((value) => value.trim().toLowerCase());
	if (!header || header.length !== headers.length || headers.some((value, index) => header[index] !== value)) {
		throw new Error("Expected columns: First Name, Last Name, Mobile, Email, ContractId.");
	}
	return records
		.filter(({ fields }) => fields.some((value) => value.trim()))
		.map(({ line, fields }) => {
			if (fields.length !== headers.length) throw new Error(`Expected five columns on line ${line}.`);
			const [firstName, lastName, mobile, email, contractIdText] = fields.map((value) => value.trim());
			const contractId = Number(contractIdText);
			if (!/^[1-9]\d*$/.test(contractIdText) || !Number.isSafeInteger(contractId)) {
				throw new Error(`Invalid contract ID on line ${line}.`);
			}
			return { line, firstName, lastName, mobile, email, contractId };
		});
}

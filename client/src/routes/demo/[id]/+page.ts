import { error } from "@sveltejs/kit";

export const ssr = true;

export const load = ({ params }) => {
	const id = Number(params.id);
	if (!Number.isFinite(id) || id <= 0) {
		throw error(400, "Invalid booking id");
	}
	return { id };
};

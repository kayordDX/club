import type { SortingState } from "@tanstack/svelte-table";

/**
 * Convert TanStack table sorting state into a QueryKit sort string.
 * Produces the same format the data-table URL encoder uses, e.g. "-slotStartDatetime,id",
 * which QueryKit accepts as-is (desc via the leading `-`).
 */
export const sortingToQueryKitSorts = (sorting: SortingState): string | undefined => {
	if (sorting.length === 0) return undefined;
	return sorting.map((s) => `${s.desc ? "-" : ""}${s.id}`).join(",");
};

/**
 * Build a QueryKit filter expression for the booking list tables.
 *
 * - `status` filters by the booking status id. Because the backend exposes Expired as a
 *   derived status, selecting Expired returns pending bookings past their expiry.
 * - `search` matches an exact booking id, but only when the text is a whole number so a
 *   half-typed/partial value doesn't silently drop every row.
 *
 * QueryKit resolves property names case-insensitively, so the camelCase names map to the
 * PascalCase DTO properties on the server.
 */
export const buildBookingFilters = ({ status, search }: { status?: number | null; search?: string | null }): string | undefined => {
	const parts: string[] = [];
	if (status != null) parts.push(`bookingStatusId == ${status}`);
	const trimmed = (search ?? "").trim();
	if (/^\d+$/.test(trimmed)) parts.push(`id == ${trimmed}`);
	return parts.length > 0 ? parts.join(" && ") : undefined;
};

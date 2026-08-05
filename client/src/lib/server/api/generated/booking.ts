/**
 * GENERATED-style server-side typed fetch — mirrors the shape of the orval
 * output (src/lib/api/generated/booking.ts) but calls the .NET API from the
 * SvelteKit server instead of the browser.
 *
 * In a real setup this file is produced by a swagger→server-fetch generator
 * (a second orval mutator/output, or a small template). Hand-written here to
 * prove the pattern for the POC. Types come from the existing orval schemas.
 */
import type { BookingDTO, BookingGetUserParams, BookingPathDTO, PaginatedListOfBookingSummaryDTO } from "$lib/api/generated/api.schemas";
import { customServerInstance } from "../client";

export const bookingGet = (id: number): Promise<BookingDTO> => customServerInstance<BookingDTO>(`/booking/${id}`, { method: "GET" });

export const bookingGetPath = (id: number): Promise<BookingPathDTO> => customServerInstance<BookingPathDTO>(`/booking/${id}/path`, { method: "GET" });

export const bookingGetUser = (params?: BookingGetUserParams): Promise<PaginatedListOfBookingSummaryDTO> => {
	const search = new URLSearchParams();
	for (const [key, value] of Object.entries(params ?? {})) {
		if (value !== undefined && value !== null) {
			search.append(key, value as string);
		}
	}
	const qs = search.toString();
	return customServerInstance<PaginatedListOfBookingSummaryDTO>(`/booking/user${qs ? `?${qs}` : ""}`, { method: "GET" });
};

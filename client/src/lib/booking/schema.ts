import { z } from "zod";

const playerSchema = z.object({
	name: z.string().trim().min(1, "Name is required"),
	cellNo: z
		.string()
		.trim()
		.min(1, "Cell No is required")
		.refine((value) => /[0-9]/.test(value), "Cell No must contain digits"),
	email: z.email("Enter a valid email address"),
	contractId: z.string().min(1, "Contract ID is required"),
});

/**
 * Shared schema for the player details form used by both the slot (booking
 * creation) and booking edit pages.
 */
export const playersSchema = z.object({
	players: z.array(playerSchema).refine((arr) => arr.length > 0, "At least one player is required"),
});

export const guestPlayersSchema = z.object({
	players: z
		.array(
			playerSchema.extend({
				cellNo: z.union([playerSchema.shape.cellNo, z.literal("")]),
				email: z.union([playerSchema.shape.email, z.literal("")]),
			})
		)
		.min(1, "At least one player is required"),
});

export type Players = z.infer<typeof playersSchema>;

export function getPlayersSchema(optionalContactDetails: boolean): z.ZodType<Players, Players> {
	return optionalContactDetails ? guestPlayersSchema : playersSchema;
}

/** Player drafts after running through the shared player schema. */
export type PlayerValues = Players["players"];

/** Payload passed to the shared booking details form on submit. */
export type BookingFormSubmitValues = {
	players: PlayerValues;
	extras: SelectedExtra[];
};

/** Submit handler signature consumed by the booking details form. */
export type BookingFormSubmitHandler = (values: BookingFormSubmitValues) => Promise<void>;

export type BookingExtraOption = {
	id: number;
	name: string;
	price: number;
	description?: string | null;
};

export type SelectedExtra = {
	id: number;
	name: string;
	price: number;
	amount: number;
};

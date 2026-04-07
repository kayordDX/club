import { createAppForm } from "$lib/components/Form";
import type { DeepKeys } from "@tanstack/svelte-form";
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

const playersSchema = z.object({
	players: z.array(playerSchema).refine((arr) => arr.length > 0, "At least one player is required"),
});

type Players = z.infer<typeof playersSchema>;

const formType = () =>
	createAppForm(() => ({
		defaultValues: {} as Players,
	}));

type FormApi = ReturnType<typeof formType>;

type SelectedExtra = {
	id: number;
	name: string;
	price: number;
	amount: number;
};

export { playersSchema, type Players, type FormApi, type SelectedExtra };

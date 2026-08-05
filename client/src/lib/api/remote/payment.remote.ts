// GENERATED from swagger.json by tools/gen-remote.mjs — do not edit manually.
// Remote functions (query/command) wrapping the orval-generated server transport.

import { query, command } from "$app/server";
import { z } from "zod";
import * as api from "$lib/server/api/generated/payment";
import type { PaymentCheckoutRequest, PaymentInitiateRequest } from "$lib/server/api/generated/server.schemas";

export const paymentResultGet = query(z.string(), async (provider) => api.paymentResultGet(provider));
export const paymentResultPost = command(z.string(), async (provider) => api.paymentResultPost(provider));
export const paymentInitiate = command(z.custom<PaymentInitiateRequest>(), async (body) => api.paymentInitiate(body));
export const paymentForm = query(z.object({ provider: z.string(), transactionId: z.string() }), async ({ provider, transactionId }) =>
	api.paymentForm(provider, transactionId)
);
export const paymentCheckout = command(z.object({ provider: z.string(), body: z.custom<PaymentCheckoutRequest>() }), async ({ provider, body }) =>
	api.paymentCheckout(provider, body)
);

import type { SelectedExtra } from "./schema";

export const getSelectedExtrasTotal = (selectedExtras: SelectedExtra[]) => selectedExtras.reduce((sum, extra) => sum + extra.price * extra.amount, 0);

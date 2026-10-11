import { goto } from "$app/navigation";
import { resolve } from "$app/paths";
import { slotAvailable } from "$lib/api/remote/slot.remote";
import { toast } from "svelte-sonner";

type AdminSlotCreationContext = {
	slug: string;
	facilityId: string;
	slotId: string;
	date: string;
	onUnavailable: () => Promise<unknown>;
};

export async function openAdminBookingCreation({ slug, facilityId, slotId, date, onUnavailable }: AdminSlotCreationContext) {
	try {
		if (!(await slotAvailable({ id: slotId, slotCount: 1 }))) {
			toast.error("Not enough slots available");
			await onUnavailable();
			return;
		}
		const query = new URLSearchParams({ slotId, date, slotCount: "1" });
		await goto(resolve(`/(protected)/outlet/[slug]/[id]/admin/bookings/new?${query}`, { slug, id: facilityId }));
	} catch {
		toast.error("Unable to verify slot availability. Please try again.");
	}
}

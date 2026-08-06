<script lang="ts">
	import { resolve } from "$app/paths";
	import type { ResolvedPathname } from "$app/types";
	import { Breadcrumb } from "@kayord/ui";
	import { HouseIcon } from "@lucide/svelte";
	import type { BookingPathDTO } from "$lib/api";
	import { getBookingPayUrl } from "$lib/booking/payUrl";
	import type { Snippet } from "svelte";

	type Props = {
		bookingId: number;
		path: BookingPathDTO;
		slotCount: number;
		children?: Snippet;
	};

	let { bookingId, path, slotCount, children }: Props = $props();

	const facilityHref = $derived.by(() => {
		if (!path) return "/";
		const date = path.slotStartDatetime?.slice(0, 10);
		return date ? `${resolve(`/outlet/${path.outletSlug}/${path.facilityId}`)}?date=${date}` : resolve(`/outlet/${path.outletSlug}/${path.facilityId}`);
	});

	const paymentHref = $derived(path ? getBookingPayUrl(bookingId, path, slotCount) : ("/" as ResolvedPathname));
</script>

<div class="m-2">
	<Breadcrumb.Root>
		<Breadcrumb.List>
			<Breadcrumb.Item>
				<Breadcrumb.Link href="/">
					<HouseIcon class="size-3" />
				</Breadcrumb.Link>
			</Breadcrumb.Item>
			<Breadcrumb.Separator />
			<Breadcrumb.Item>
				<Breadcrumb.Link href={resolve(`/outlet/${path.outletSlug}`)} class="text-xs">
					{path.outletName}
				</Breadcrumb.Link>
			</Breadcrumb.Item>
			<Breadcrumb.Separator />
			<Breadcrumb.Item>
				<Breadcrumb.Link href={facilityHref} class="text-xs">
					{path.facilityName}
				</Breadcrumb.Link>
			</Breadcrumb.Item>
			<Breadcrumb.Separator />
			<Breadcrumb.Item>
				<Breadcrumb.Page class="text-xs">Player Details</Breadcrumb.Page>
			</Breadcrumb.Item>
			<Breadcrumb.Separator />
			<Breadcrumb.Item>
				<Breadcrumb.Link href={paymentHref} class="text-xs">Payment</Breadcrumb.Link>
			</Breadcrumb.Item>
		</Breadcrumb.List>
	</Breadcrumb.Root>
	{@render children?.()}
</div>

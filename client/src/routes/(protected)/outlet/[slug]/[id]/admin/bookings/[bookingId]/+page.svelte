<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { toast } from "svelte-sonner";

	import { BookingStatusEnum } from "$lib/api";
	import { adminBookingGet, adminBookingUpdate, adminBookingUpdateStatus, adminBookingGetAll } from "$lib/api/remote/admin.remote";
	import { BOOKING_STATUS_OPTIONS, statusBadgeVariant, statusLabel } from "$lib/booking/status";
	import { formatCurrency, formatDate } from "$lib/booking/format";
	import { buildExtras, buildPlayers } from "$lib/booking/bookingForm";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import BookingDetailsForm from "$lib/components/booking/BookingDetailsForm.svelte";
	import { Alert, Badge, Button, Card, DropdownMenu } from "@kayord/ui";
	import { ChevronLeftIcon, SettingsIcon, ShieldCheckIcon } from "@lucide/svelte";

	const facilityId = Number(page.params.id) || 0;
	const bookingId = Number(page.params.bookingId) || 0;

	const booking = await adminBookingGet({ facilityId, id: bookingId });

	const listHref = resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings`);

	const slotId = booking.slotContractBookings?.[0]?.slotContract?.slotId ?? "";
	const slotStartDatetime = booking.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime ?? null;
	const slotEndDatetime = booking.slotContractBookings?.[0]?.slotContract?.slot?.endDatetime ?? null;
	const slotDate = slotStartDatetime?.slice(0, 10) ?? "";
	const ownPlayerCount = booking.slotContractBookings?.length ?? 0;

	let isSubmitting = $state(false);

	const refreshAll = () => {
		void adminBookingGet({ facilityId, id: bookingId }).refresh();
		void adminBookingGetAll({ facilityId }).refresh();
	};

	const changeStatus = async (status: BookingStatusEnum) => {
		try {
			await adminBookingUpdateStatus({ facilityId, id: bookingId, body: { status } });
			toast.success(`Status changed to ${statusLabel(status)}`);
			refreshAll();
		} catch (error) {
			toast.error(error instanceof Error ? error.message : "Failed to change status");
		}
	};

	const handleSubmit = async ({
		players,
		extras,
	}: {
		players: { name: string; cellNo: string; email: string; contractId: string }[];
		extras: { id: number; amount: number }[];
	}) => {
		try {
			isSubmitting = true;
			await adminBookingUpdate({
				facilityId,
				id: bookingId,
				body: {
					bookings: players.map((player) => ({
						slotId,
						slotContractId: Number(player.contractId),
						name: player.name,
						cellphone: player.cellNo,
						email: player.email,
					})),
					extras: extras.map((extra) => ({
						extraId: extra.id,
						amount: extra.amount,
					})),
				},
			});
			toast.success("Booking updated");
			refreshAll();
		} catch (error) {
			const message = error instanceof Error && error.cause ? String(error.cause) : error instanceof Error ? error.message : "Failed to update booking.";
			toast.error(message);
		} finally {
			isSubmitting = false;
		}
	};
</script>

<div class="m-2 flex flex-col gap-6">
	<div class="flex items-start justify-between gap-4">
		<PageHeading title="Manage Booking" description={`Booking #${bookingId}`} icon={ShieldCheckIcon} />

		<Button href={listHref} variant="outline">
			<ChevronLeftIcon class="size-4" />
			Back to bookings
		</Button>
	</div>

	<Alert.Root variant="default">
		<Alert.Title>Manager access</Alert.Title>
		<Alert.Description>You can freely change this booking's status and edit player and extra details, regardless of status.</Alert.Description>
	</Alert.Root>

	<div class="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
		<Card.Root>
			<Card.Header>
				<Card.Title>Booking</Card.Title>
			</Card.Header>
			<Card.Content class="space-y-3">
				<div>
					<div class="text-muted-foreground text-sm">Date</div>
					<div>{formatDate(slotStartDatetime)}</div>
				</div>
				<div>
					<div class="text-muted-foreground text-sm">Status</div>
					<div class="flex items-center gap-2">
						<Badge variant={statusBadgeVariant(booking.bookingStatus?.id)}>
							{booking.bookingStatus?.name ?? "—"}
						</Badge>
						<DropdownMenu.Root>
							<DropdownMenu.Trigger>
								{#snippet child({ props })}
									<Button {...props} variant="outline" size="sm">
										<SettingsIcon class="size-4" />
										Change status
									</Button>
								{/snippet}
							</DropdownMenu.Trigger>
							<DropdownMenu.Content>
								{#each BOOKING_STATUS_OPTIONS as option (option.value)}
									<DropdownMenu.Item onclick={() => changeStatus(option.value)}>
										{option.label}
									</DropdownMenu.Item>
								{/each}
							</DropdownMenu.Content>
						</DropdownMenu.Root>
					</div>
				</div>
				<div>
					<div class="text-muted-foreground text-sm">Booked by</div>
					<div>{booking.user ? `${booking.user.firstName} ${booking.user.lastName}` : "—"}</div>
				</div>
			</Card.Content>
		</Card.Root>

		<Card.Root>
			<Card.Header>
				<Card.Title>Payment</Card.Title>
			</Card.Header>
			<Card.Content class="space-y-3">
				<div>
					<div class="text-muted-foreground text-sm">Paid</div>
					<div>{booking.isPaid ? "Yes" : "No"}</div>
				</div>
				<div>
					<div class="text-muted-foreground text-sm">Amount paid</div>
					<div>{formatCurrency(booking.amountPaid)}</div>
				</div>
				<div>
					<div class="text-muted-foreground text-sm">Amount outstanding</div>
					<div>{formatCurrency(booking.amountOutstanding)}</div>
				</div>
			</Card.Content>
		</Card.Root>
	</div>

	<BookingDetailsForm
		title="Edit booking details"
		description="Update player details and extras. Changes are applied immediately."
		submitLabel="Save changes"
		submittingLabel="Saving..."
		{isSubmitting}
		backHref={listHref}
		backLabel="Back to bookings"
		{slotId}
		{facilityId}
		date={slotDate}
		dateLabel={formatDate(slotStartDatetime)}
		{slotStartDatetime}
		{slotEndDatetime}
		initialPlayers={buildPlayers(booking)}
		initialExtras={buildExtras(booking)}
		{ownPlayerCount}
		onSubmit={handleSubmit}
	>
		{#snippet statusExtra()}
			<Badge variant={statusBadgeVariant(booking.bookingStatus?.id)}>
				Status: {booking.bookingStatus?.name}
			</Badge>
		{/snippet}
	</BookingDetailsForm>
</div>

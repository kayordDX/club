<script lang="ts">
	import { page } from "$app/state";
	import { resolve } from "$app/paths";
	import { useQueryClient } from "@tanstack/svelte-query";
	import { toast } from "svelte-sonner";

	import { BookingStatusEnum, createAdminBookingGet, createAdminBookingUpdate, createAdminBookingUpdateStatus } from "$lib/api";
	import { BOOKING_STATUS_OPTIONS, statusBadgeVariant, statusLabel } from "$lib/admin/booking";
	import { formatCurrency, formatDate } from "$lib/booking/format";
	import { buildExtras, buildPlayers } from "$lib/booking/bookingForm";
	import PageHeading from "$lib/components/PageHeading.svelte";
	import Query from "$lib/components/Query.svelte";
	import BookingDetailsForm from "$lib/components/booking/BookingDetailsForm.svelte";
	import { Alert, Badge, Button, Card, DropdownMenu } from "@kayord/ui";
	import { ChevronLeftIcon, SettingsIcon, ShieldCheckIcon } from "@lucide/svelte";

	const facilityId = $derived(Number(page.params.id) || 0);
	const bookingId = $derived(Number(page.params.bookingId) || 0);

	const query = createAdminBookingGet(
		() => facilityId,
		() => bookingId,
		() => ({ query: { enabled: facilityId > 0 && bookingId > 0 } })
	);
	const booking = $derived(query.data);

	const listHref = $derived(resolve(`/outlet/${page.params.slug}/${page.params.id}/admin/bookings`));

	const slotId = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slotId ?? "");
	const slotStartDatetime = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slot?.startDatetime ?? null);
	const slotEndDatetime = $derived(booking?.slotContractBookings?.[0]?.slotContract?.slot?.endDatetime ?? null);
	const slotDate = $derived(slotStartDatetime?.slice(0, 10) ?? "");
	const ownPlayerCount = $derived(booking?.slotContractBookings?.length ?? 0);

	const queryClient = useQueryClient();
	const invalidateAll = () => {
		queryClient.invalidateQueries({ queryKey: [`/admin/facility/${facilityId}/booking`] });
		queryClient.invalidateQueries({ queryKey: [`/admin/facility/${facilityId}/booking/${bookingId}`] });
	};

	const statusMutation = createAdminBookingUpdateStatus();
	const changeStatus = async (status: BookingStatusEnum) => {
		try {
			await statusMutation.mutateAsync({
				facilityId,
				id: bookingId,
				data: { status },
			});
			toast.success(`Status changed to ${statusLabel(status)}`);
			invalidateAll();
		} catch (error) {
			toast.error(error instanceof Error ? error.message : "Failed to change status");
		}
	};

	const updateMutation = createAdminBookingUpdate();
	const handleSubmit = async ({
		players,
		extras,
	}: {
		players: { name: string; cellNo: string; email: string; contractId: string }[];
		extras: { id: number; amount: number }[];
	}) => {
		try {
			await updateMutation.mutateAsync({
				facilityId,
				id: bookingId,
				data: {
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
			invalidateAll();
		} catch (error) {
			const message = error instanceof Error && error.cause ? String(error.cause) : error instanceof Error ? error.message : "Failed to update booking.";
			toast.error(message);
		}
	};
</script>

<Query {query} emptyText="Booking not found">
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
							<Badge variant={statusBadgeVariant(booking?.bookingStatus?.id)}>
								{booking?.bookingStatus?.name ?? "—"}
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
						<div>{booking?.user ? `${booking.user.firstName} ${booking.user.lastName}` : "—"}</div>
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
						<div>{booking?.isPaid ? "Yes" : "No"}</div>
					</div>
					<div>
						<div class="text-muted-foreground text-sm">Amount paid</div>
						<div>{formatCurrency(booking?.amountPaid)}</div>
					</div>
					<div>
						<div class="text-muted-foreground text-sm">Amount outstanding</div>
						<div>{formatCurrency(booking?.amountOutstanding)}</div>
					</div>
				</Card.Content>
			</Card.Root>
		</div>

		<BookingDetailsForm
			title="Edit booking details"
			description="Update player details and extras. Changes are applied immediately."
			submitLabel="Save changes"
			submittingLabel="Saving..."
			isSubmitting={updateMutation.isPending}
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
				<Badge variant={statusBadgeVariant(booking?.bookingStatus?.id)}>
					Status: {booking?.bookingStatus?.name}
				</Badge>
			{/snippet}
		</BookingDetailsForm>
	</div>
</Query>

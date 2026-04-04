<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import { createBookingCreate } from "$lib/api";

	const bookingMutation = createBookingCreate();

	import { createSlotGetAll, createSlotGetContracts } from "$lib/api";
	import { createAppForm, Form } from "$lib/components/Form";
	import Query from "$lib/components/Query.svelte";
	import { Badge, Button, Card, Empty } from "@kayord/ui";
	import {
		CalendarDaysIcon,
		ChevronLeftIcon,
		Clock3Icon,
		CreditCardIcon,
		UserRoundIcon,
	} from "@lucide/svelte";
	import { toast } from "svelte-sonner";
	import { z } from "zod";

	type BookingPlayer = {
		name: string;
		cellNo: string;
		email: string;
		contractId: string;
	};

	const slug = $derived(page.params.slug ?? "");
	const facilityId = $derived(Number(page.params.id) || 0);
	const slotId = $derived(page.params.slot ?? "");
	const slotCount = $derived(Math.max(1, Number(page.url.searchParams.get("slotCount")) || 1));
	const selectedDate = $derived(page.url.searchParams.get("date") ?? "");

	const slotQuery = createSlotGetAll(
		() => ({
			facilityId,
			date: selectedDate,
		}),
		() => ({ query: { enabled: facilityId > 0 && !!selectedDate } })
	);

	const contractsQuery = createSlotGetContracts(() => slotId);

	const slot = $derived(slotQuery.data?.find((item) => item.id === slotId));
	const contracts = $derived(contractsQuery.data ?? []);
	const contractItems = $derived(
		contracts.map((contract) => ({
			value: contract.id.toString(),
			label: `${contract.contractName} ${contract.description} - R ${contract.price.toFixed(2)}`,
		}))
	);

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
		players: z
			.array(playerSchema)
			.refine((arr) => arr.length > 0, "At least one player is required"),
	});

	const formatCurrency = (value: number) =>
		new Intl.NumberFormat("en-ZA", {
			style: "currency",
			currency: "ZAR",
		}).format(value);

	const formatTime = (datetime?: string | null) => {
		if (!datetime) return "";
		return new Date(datetime).toLocaleTimeString("en-ZA", {
			hour: "2-digit",
			minute: "2-digit",
			hour12: false,
		});
	};

	function createPlayers(count: number) {
		return Array.from({ length: count }, () => ({
			name: "",
			cellNo: "",
			email: "",
			contractId: "",
		}));
	}

	const form = createAppForm(() => ({
		defaultValues: {
			players: createPlayers(slotCount) as BookingPlayer[],
		},
		validators: {
			onChange: playersSchema,
		},
		onSubmit: async ({ value }) => {
			try {
				const bookings = value.players.map((player) => ({
					slotId,
					slotContractId: Number(player.contractId),
					name: player.name,
					cellphone: player.cellNo,
					email: player.email,
				}));

				const bookingResponse = await bookingMutation.mutateAsync({
					data: {
						bookings: bookings,
					},
				});

				await goto(resolve(`/outlet/${slug}/book/${facilityId}/booking/${bookingResponse.id}/pay`));
			} catch {
				toast.error("Failed to create booking. Please try again.");
			} finally {
				toast.info("Created booking");
			}
		},
	}));

	const getPriceFromContractId = (contractId: string) =>
		contracts.find((c) => c.id === Number(contractId))?.price ?? 0;

	// Form reactivity
	const players = form.useStore((state) => state.values.players);

	const totalPrice = $derived(
		(players.current ?? [])
			.map((c) => getPriceFromContractId(c.contractId))
			.reduce((sum, price) => sum + price, 0)
	);
</script>

<div class="mx-auto flex w-full flex-col gap-6">
	<div class="grid gap-4">
		<Card.Root class="border-border/60 overflow-hidden border shadow-sm">
			<Card.Header class="border-border/60  border-b">
				<div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
					<div class="space-y-2">
						<Card.Title class="text-2xl">Add each player's details</Card.Title>
						<Card.Description class="max-w-2xl text-sm leading-6">
							Capture the booking information for all {slotCount} players, review the summary, and then
							choose how you want to pay.
						</Card.Description>
					</div>
				</div>
			</Card.Header>

			<Query
				query={contractsQuery}
				emptyText="No booking contracts are available for this slot yet."
			>
				{#if !selectedDate}
					<Card.Content class="p-6">
						<Empty.Root>
							<Empty.Header>
								<Empty.Title>Select a date first</Empty.Title>
								<Empty.Description>
									Choose a date on the facility page before continuing with player details.
								</Empty.Description>
							</Empty.Header>
							<Empty.Content>
								<Button href={resolve(`/outlet/${slug}/book/${facilityId}`)} variant="outline">
									<ChevronLeftIcon class="size-4" />
									Back to slots
								</Button>
							</Empty.Content>
						</Empty.Root>
					</Card.Content>
				{:else}
					<Form {form}>
						<Card.Content class="space-y-6 p-6">
							<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
								<div class="rounded-2xl border p-4">
									<div
										class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
									>
										<CalendarDaysIcon class="size-4" />
										Date
									</div>
									<p class="mt-3 text-sm font-semibold">{selectedDate}</p>
								</div>
								<div class="rounded-2xl border p-4">
									<div
										class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
									>
										<Clock3Icon class="size-4" />
										Time
									</div>
									<p class="mt-3 text-sm font-semibold">
										{slot
											? `${formatTime(slot.startDatetime)} - ${formatTime(slot.endDatetime)}`
											: "Selected slot"}
									</p>
								</div>
								<div class="rounded-2xl border p-4">
									<div
										class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
									>
										<UserRoundIcon class="size-4" />
										Players
									</div>
									<p class="mt-3 text-sm font-semibold">{slotCount} total</p>
								</div>
								<div class="rounded-2xl border p-4">
									<div
										class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
									>
										<CreditCardIcon class="size-4" />
										Total
									</div>
									<p class="mt-3 text-sm font-semibold">
										{formatCurrency(totalPrice)}
									</p>
								</div>
							</div>

							<div class="space-y-4">
								<div class="flex items-center justify-between gap-4">
									<div class="mt-4">
										<h2 class="text-lg font-semibold">User information</h2>
										<p class="text-muted-foreground text-sm">
											Add the contact details for each user included in this booking.
										</p>
									</div>
									<Badge variant="outline">{slotCount} users</Badge>
								</div>

								<div class="space-y-4">
									<form.Field name="players">
										{#snippet children(field)}
											<div class="flex flex-col gap-4">
												{#each field.state.value as _, index (index)}
													<Card.Root>
														<Card.Header class="pb-4">
															<div class="flex items-center justify-between gap-4">
																<div>
																	<Card.Title class="text-base">User {index + 1}</Card.Title>
																	<Card.Description
																		>Enter the details for this player.</Card.Description
																	>
																</div>
																<Badge variant="secondary">Required</Badge>
															</div>
														</Card.Header>
														<Card.Content class="grid gap-4 md:grid-cols-2">
															<form.AppField name={`players[${index}].contractId`}>
																{#snippet children(field)}
																	<field.Select label="Contract" items={contractItems} />
																{/snippet}
															</form.AppField>
															<form.AppField name={`players[${index}].name`}>
																{#snippet children(field)}
																	<field.Input label="Name" placeholder="Player full name" />
																{/snippet}
															</form.AppField>
															<form.AppField name={`players[${index}].cellNo`}>
																{#snippet children(field)}
																	<field.Input label="Cell No" placeholder="e.g. 082 123 4567" />
																{/snippet}
															</form.AppField>
															<form.AppField name={`players[${index}].email`}>
																{#snippet children(field)}
																	<field.Input
																		label="Email"
																		type="email"
																		placeholder="player@email.com"
																	/>
																{/snippet}
															</form.AppField>
														</Card.Content>
													</Card.Root>
												{/each}
											</div>
										{/snippet}
									</form.Field>
								</div>
							</div>
						</Card.Content>
						<Card.Footer class=" flex justify-between border-t">
							<Button href={resolve(`/outlet/${slug}/book/${facilityId}`)} variant="ghost">
								<ChevronLeftIcon class="size-4" />
								Back to slots
							</Button>
							<Button type="submit">Book</Button>
						</Card.Footer>
					</Form>
				{/if}
			</Query>
		</Card.Root>
	</div>
</div>

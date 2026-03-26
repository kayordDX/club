<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import {
		bookingFlow,
		type BookingPaymentMethod,
		type BookingPlayer,
	} from "$lib/stores/booking-flow.svelte";
	import { createSlotGetAll, createSlotGetContracts, type SlotGetAllResponse } from "$lib/api";
	import Query from "$lib/components/Query.svelte";
	import { Badge, Button, Card, Empty, Input, ToggleGroup } from "@kayord/ui";
	import {
		CalendarDaysIcon,
		ChevronLeftIcon,
		Clock3Icon,
		CreditCardIcon,
		UserRoundIcon,
		WalletIcon,
	} from "@lucide/svelte";
	import { z } from "zod";

	type PlayerFieldErrors = Partial<Record<keyof BookingPlayer, string>>;

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

	const playerSchema = z.object({
		name: z.string().trim().min(1, "Name is required"),
		cellNo: z
			.string()
			.trim()
			.min(1, "Cell No is required")
			.refine((value) => /[0-9]/.test(value), "Cell No must contain digits"),
		email: z.email("Enter a valid email address"),
	});

	let players = $state<BookingPlayer[]>([]);
	let playerErrors = $state<Record<number, PlayerFieldErrors>>({});
	let selectedContractId = $state<number | null>(null);
	let paymentMethod = $state<BookingPaymentMethod>("credit-card");

	const selectedContract = $derived(
		contracts.find((contract) => contract.id === selectedContractId) ?? contracts[0] ?? null
	);
	const totalPrice = $derived((selectedContract?.price ?? 0) * slotCount);
	const paymentOptions = $derived.by(() => {
		const options: Array<{
			value: BookingPaymentMethod;
			label: string;
			description: string;
			icon: typeof CreditCardIcon;
			disabled: boolean;
		}> = [
			{
				value: "credit-card",
				label: "Credit card",
				description: "Pay online and secure the booking instantly.",
				icon: CreditCardIcon,
				disabled: false,
			},
			{
				value: "pay-later",
				label: "Pay later",
				description: "Reserve now and settle payment at the outlet.",
				icon: WalletIcon,
				disabled: selectedContract ? !selectedContract.canPayLater : false,
			},
		];

		return options;
	});
	const selectedPayment = $derived(
		paymentOptions.find((option) => option.value === paymentMethod) ?? paymentOptions[0]
	);

	const syncPlayers = (count: number) => {
		const nextPlayers = createPlayers(count, players);
		players.length = 0;
		players.push(...nextPlayers);

		const nextErrors: Record<number, PlayerFieldErrors> = {};
		for (let index = 0; index < count; index += 1) {
			if (playerErrors[index]) {
				nextErrors[index] = playerErrors[index];
			}
		}
		playerErrors = nextErrors;
	};

	$effect(() => {
		if (players.length !== slotCount) {
			syncPlayers(slotCount);
		}
	});

	$effect(() => {
		if (!selectedContractId && contracts.length > 0) {
			selectedContractId = contracts[0].id;
		}
	});

	$effect(() => {
		if (paymentMethod === "pay-later" && selectedPayment?.disabled) {
			paymentMethod = "credit-card";
		}
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

	const createLabel = (slotData?: SlotGetAllResponse) => {
		if (!slotData) return "Selected slot";
		const start = formatTime(slotData.startDatetime);
		const end = formatTime(slotData.endDatetime);
		return `${slotData.resourceName} - ${start} to ${end}`;
	};

	function createPlayers(count: number, existing: BookingPlayer[] = []) {
		return Array.from({ length: count }, (_, index) => ({
			name: existing[index]?.name ?? "",
			cellNo: existing[index]?.cellNo ?? "",
			email: existing[index]?.email ?? "",
		}));
	}

	const updatePlayer = (index: number, field: keyof BookingPlayer, value: string) => {
		players[index][field] = value;
		if (playerErrors[index]?.[field]) {
			playerErrors[index] = {
				...playerErrors[index],
				[field]: undefined,
			};
		}
	};

	const validatePlayers = () => {
		const nextErrors: Record<number, PlayerFieldErrors> = {};
		let hasErrors = false;

		players.forEach((player, index) => {
			const result = playerSchema.safeParse(player);
			if (!result.success) {
				hasErrors = true;
				nextErrors[index] = {};
				for (const issue of result.error.issues) {
					const field = issue.path[0];
					if (typeof field === "string" && !(field in nextErrors[index])) {
						nextErrors[index][field as keyof BookingPlayer] = issue.message;
					}
				}
			}
		});

		playerErrors = nextErrors;
		return !hasErrors;
	};

	const book = async () => {
		if (!selectedContract || !slot) return;

		bookingFlow.set({
			bookingId: 0,
			slotId,
			facilityId,
			outletSlug: slug,
			quantity: slotCount,
			selectedDate,
			selectedSlotLabel: createLabel(slot),
			selectedContract,
			players: players.map((player) => ({ ...player })),
			paymentMethod,
			contactEmail: players[0]?.email ?? "",
			totalPrice,
			createdAt: new Date().toISOString(),
			updatedAt: new Date().toISOString(),
		});

		await goto(resolve(`/outlet/${slug}/book/${facilityId}/booking/0/pay`));
	};
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
								<div>
									<h2 class="text-lg font-semibold">Choose a rate</h2>
									<p class="text-muted-foreground text-sm">
										Pick the contract that matches this booking before continuing.
									</p>
								</div>
							</div>
							<div class="w-full">
								<ToggleGroup.Root
									type="single"
									class="w-full border"
									onValueChange={(value) => (selectedContractId = Number(value))}
								>
									{#each contracts as contract (contract.id)}
										<ToggleGroup.Item
											value={contract.id.toString()}
											class="flex h-fit flex-1 flex-col p-2"
										>
											<div class="text-muted-foreground text-xs">
												{contract.contractName}
											</div>
											<div>
												{contract.description}
											</div>
											<Badge>
												R {contract.price.toFixed(2)}
											</Badge>
										</ToggleGroup.Item>
									{/each}
								</ToggleGroup.Root>
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
								{#each players as player, index (index)}
									<Card.Root>
										<Card.Header class="pb-4">
											<div class="flex items-center justify-between gap-4">
												<div>
													<Card.Title class="text-base">User {index + 1}</Card.Title>
													<Card.Description>Enter the details for this player.</Card.Description>
												</div>
												<Badge variant="secondary">Required</Badge>
											</div>
										</Card.Header>
										<Card.Content class="grid gap-4 md:grid-cols-3">
											<label class="space-y-2">
												<span class="text-sm font-medium">Name</span>
												<Input
													placeholder="Player full name"
													value={player.name}
													oninput={(event) =>
														updatePlayer(
															index,
															"name",
															(event.currentTarget as HTMLInputElement).value
														)}
												/>
												{#if playerErrors[index]?.name}
													<p class="text-destructive text-sm">{playerErrors[index]?.name}</p>
												{/if}
											</label>

											<label class="space-y-2">
												<span class="text-sm font-medium">Cell No</span>
												<Input
													placeholder="e.g. 082 123 4567"
													value={player.cellNo}
													oninput={(event) =>
														updatePlayer(
															index,
															"cellNo",
															(event.currentTarget as HTMLInputElement).value
														)}
												/>
												{#if playerErrors[index]?.cellNo}
													<p class="text-destructive text-sm">{playerErrors[index]?.cellNo}</p>
												{/if}
											</label>

											<label class="space-y-2">
												<span class="text-sm font-medium">Email</span>
												<Input
													type="email"
													placeholder="player@email.com"
													value={player.email}
													oninput={(event) =>
														updatePlayer(
															index,
															"email",
															(event.currentTarget as HTMLInputElement).value
														)}
												/>
												{#if playerErrors[index]?.email}
													<p class="text-destructive text-sm">{playerErrors[index]?.email}</p>
												{/if}
											</label>
										</Card.Content>
									</Card.Root>
								{/each}
							</div>
						</div>
					</Card.Content>
					<Card.Footer class=" flex justify-between border-t">
						<Button href={resolve(`/outlet/${slug}/book/${facilityId}`)} variant="ghost">
							<ChevronLeftIcon class="size-4" />
							Back to slots
						</Button>
						<Button onclick={book} disabled={!selectedContract}>Book</Button>
					</Card.Footer>
				{/if}
			</Query>
		</Card.Root>
	</div>
</div>

<script lang="ts">
	import type { ResolvedPathname } from "$app/types";
	import { untrack, type Snippet } from "svelte";

	import { createSlotGetAll, createSlotGetContracts } from "$lib/api";
	import { auth } from "$lib/stores/auth.svelte";
	import { applyFirstPlayerContract, applyProfileToPlayer, type PlayerDraft } from "$lib/booking/players";
	import { formatCurrency, formatTime } from "$lib/booking/format";
	import { getSelectedExtrasTotal } from "$lib/booking/pricing";
	import { playersSchema, type Players, type BookingFormSubmitHandler, type SelectedExtra } from "$lib/booking/schema";
	import { createAppForm, Form } from "$lib/components/Form";
	import Query from "$lib/components/Query.svelte";
	import Extras from "$lib/components/booking/Extras.svelte";
	import { Badge, Button, Card } from "@kayord/ui";
	import { CalendarDaysIcon, ChevronLeftIcon, Clock3Icon, CreditCardIcon, PlusIcon, Trash2Icon, UserRoundIcon } from "@lucide/svelte";

	type Props = {
		/** Card + form copy. */
		title: string;
		description: string;
		submitLabel: string;
		/** Optional pending label, shown while submitting. Defaults to "Saving...". */
		submittingLabel?: string;
		isSubmitting: boolean;
		backHref: ResolvedPathname;
		backLabel: string;
		/** Optional header content, e.g. a countdown timer. */
		headerExtra?: Snippet;
		/** Optional header content aligned to the right, e.g. a status badge. */
		statusExtra?: Snippet;
		/** Slot being booked/edited. */
		slotId: string;
		facilityId: number;
		/** Date (YYYY-MM-DD) used for the slot availability query. */
		date: string;
		/** Human readable date shown in the summary. */
		dateLabel: string;
		/** Datetime values shown in the summary until the slot query resolves. */
		slotStartDatetime?: string | null;
		slotEndDatetime?: string | null;
		/** Initial player drafts. */
		initialPlayers: PlayerDraft[];
		/** Initial selected extras. */
		initialExtras: SelectedExtra[];
		/**
		 * Players on this slot that already belong to the current booking. They are
		 * added back to the slot's booked count so the capacity calculation is
		 * correct when editing. Defaults to 0 (create flow).
		 */
		ownPlayerCount?: number;
		/** Called with the finalised players + extras on submit. */
		onSubmit: BookingFormSubmitHandler;
	};

	let {
		title,
		description,
		submitLabel,
		submittingLabel = "Saving...",
		isSubmitting,
		backHref,
		backLabel,
		headerExtra,
		statusExtra,
		slotId,
		facilityId,
		date,
		dateLabel,
		slotStartDatetime,
		slotEndDatetime,
		initialPlayers,
		initialExtras,
		ownPlayerCount = 0,
		onSubmit,
	}: Props = $props();

	const contractsQuery = createSlotGetContracts(() => slotId);
	const contracts = $derived(contractsQuery.data ?? []);
	const contractItems = $derived(
		contracts.map((contract) => ({
			value: contract.id.toString(),
			label: `${contract.contractName} ${contract.description} - R ${contract.price.toFixed(2)}`,
		}))
	);

	const slotsQuery = createSlotGetAll(
		() => ({ facilityId, date }),
		() => ({ query: { enabled: facilityId > 0 && !!date } })
	);
	const slot = $derived(slotsQuery.data?.find((item) => item.id === slotId));
	// The slot query's booked count includes this booking's own players when
	// editing, so add them back to get the number of players that can still be added.
	const remainingSlots = $derived(slot ? slot.total - slot.booked + ownPlayerCount : undefined);

	const summaryStart = $derived(slot?.startDatetime ?? slotStartDatetime);
	const summaryEnd = $derived(slot?.endDatetime ?? slotEndDatetime);

	let selectedExtras: Array<SelectedExtra> = $state(untrack(() => initialExtras));

	const form = createAppForm(() => ({
		defaultValues: {
			players: initialPlayers,
		} satisfies Players,
		validators: {
			onChange: playersSchema,
		},
		onSubmit: async ({ value }) => {
			await onSubmit({ players: value.players, extras: selectedExtras });
		},
	}));

	const players = form.useStore((state) => state.values.players);
	const playerCount = $derived(players.current?.length ?? 0);

	const addPlayer = () => {
		const firstContractId = players.current?.[0]?.contractId ?? "";
		form.setFieldValue("players", (prev) => [...prev, { name: "", cellNo: "", email: "", contractId: firstContractId }]);
	};

	const removePlayer = (index: number) => {
		form.setFieldValue("players", (prev) => prev.filter((_, i) => i !== index));
	};

	// When the first player picks a contract, fill in any other players that don't
	// have a contract yet. Players that already have a contract are never overridden.
	let lastFirstContractId = $state("");
	$effect(() => {
		const firstContractId = players.current?.[0]?.contractId ?? "";
		if (firstContractId && firstContractId !== lastFirstContractId) {
			lastFirstContractId = firstContractId;
			form.setFieldValue("players", (prev) => applyFirstPlayerContract(prev));
		}
	});

	const totalPrice = $derived(
		(players.current ?? [])
			.map((player) => contracts.find((contract) => contract.id === Number(player.contractId))?.price ?? 0)
			.reduce((sum, price) => sum + price, 0) + getSelectedExtrasTotal(selectedExtras)
	);
</script>

<Card.Root class="border-border/60 overflow-hidden border shadow-sm">
	<Card.Header class="border-border/60 border-b">
		<div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
			<div class="space-y-2">
				<Card.Title class="text-2xl">{title}</Card.Title>
				<Card.Description class="max-w-2xl text-sm leading-6">{description}</Card.Description>
				{@render headerExtra?.()}
			</div>
			{@render statusExtra?.()}
		</div>
	</Card.Header>

	<Query query={contractsQuery} emptyText="No booking contracts are available for this slot yet.">
		<Form {form}>
			<Card.Content class="space-y-6 p-6">
				<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
					<div class="rounded-2xl border p-4">
						<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
							<CalendarDaysIcon class="size-4" />
							Date
						</div>
						<p class="mt-3 text-sm font-semibold">{dateLabel}</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
							<Clock3Icon class="size-4" />
							Time
						</div>
						<p class="mt-3 text-sm font-semibold">{formatTime(summaryStart)} - {formatTime(summaryEnd)}</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
							<UserRoundIcon class="size-4" />
							Players
						</div>
						<p class="mt-3 text-sm font-semibold">{playerCount} total</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase">
							<CreditCardIcon class="size-4" />
							Total
						</div>
						<p class="mt-3 text-sm font-semibold">{formatCurrency(totalPrice)}</p>
					</div>
				</div>

				<div class="space-y-4">
					<div class="flex flex-wrap items-center justify-between gap-4">
						<div>
							<h2 class="text-lg font-semibold">User information</h2>
							<p class="text-muted-foreground text-sm">Manage the contact details for each user included in this booking.</p>
						</div>
						<div class="flex flex-wrap items-center gap-2">
							{#if remainingSlots !== undefined}
								<span class="text-muted-foreground text-sm">{remainingSlots} of {slot?.total} slot(s) remaining</span>
							{/if}
							<Badge variant="outline">{playerCount} users</Badge>
							<Button variant="outline" size="sm" onclick={addPlayer} disabled={remainingSlots !== undefined && playerCount >= remainingSlots}>
								<PlusIcon class="size-4" />
								Add player
							</Button>
						</div>
					</div>

					<div class="space-y-4">
						<form.Field name="players">
							{#snippet children(field)}
								<div class="flex flex-col gap-4">
									{#each field.state.value as player, index (index)}
										<Card.Root>
											<Card.Header class="pb-4">
												<div class="flex items-center justify-between gap-4">
													<Card.Title class="text-base">{player.name || `User ${index + 1}`}</Card.Title>
													<div class="flex items-center gap-2">
														<Button
															variant="outline"
															size="sm"
															class="h-6 px-2 text-xs"
															onclick={() => form.setFieldValue(`players[${index}]`, (current) => applyProfileToPlayer(current, auth.user?.profile))}
														>
															Me
														</Button>
														<Button
															variant="ghost"
															size="sm"
															class="h-6 px-2 text-xs"
															onclick={() => removePlayer(index)}
															disabled={(field.state.value?.length ?? 0) <= 1}
															aria-label={`Remove player ${index + 1}`}
														>
															<Trash2Icon class="size-3" />
														</Button>
													</div>
												</div>
											</Card.Header>
											<Card.Content>
												<div class="grid gap-4 md:grid-cols-2">
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
															<field.Input label="Email" type="text" placeholder="player@email.com" />
														{/snippet}
													</form.AppField>
												</div>
											</Card.Content>
										</Card.Root>
									{/each}
								</div>
							{/snippet}
						</form.Field>
					</div>
				</div>

				<div class="space-y-4">
					<div class="flex items-center justify-between gap-4">
						<div>
							<h2 class="text-lg font-semibold">Extras</h2>
							<p class="text-muted-foreground text-sm">Add or remove extras for this booking.</p>
						</div>
					</div>
					<Extras {facilityId} bind:selectedExtras />
				</div>
			</Card.Content>
			<Card.Footer class="flex justify-between border-t">
				<Button href={backHref} variant="ghost">
					<ChevronLeftIcon class="size-4" />
					{backLabel}
				</Button>
				<div class="flex items-center gap-3">
					<span class="text-muted-foreground text-sm">Total: {formatCurrency(totalPrice)}</span>
					<Button type="submit" disabled={isSubmitting}>{isSubmitting ? submittingLabel : submitLabel}</Button>
				</div>
			</Card.Footer>
		</Form>
	</Query>
</Card.Root>

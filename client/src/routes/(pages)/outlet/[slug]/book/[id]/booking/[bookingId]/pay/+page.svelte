<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";

	import { Badge, Button, Card, Table, ToggleGroup, RadioGroup, Label } from "@kayord/ui";
	import {
		CalendarDaysIcon,
		ChevronLeftIcon,
		Clock3Icon,
		CreditCardIcon,
		UserRoundIcon,
	} from "@lucide/svelte";

	const slug = $derived(page.params.slug ?? "");
	const facilityId = $derived(Number(page.params.id) || 0);
</script>

<div class="mx-auto flex w-full flex-col gap-6">
	<div class="grid gap-4">
		<Card.Root class="border-border/60 overflow-hidden border shadow-sm">
			<Card.Header class="border-border/60  border-b">
				<div class="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
					<div class="space-y-2">
						<Card.Title class="text-2xl">Payment details</Card.Title>
						<Card.Description class="max-w-2xl text-sm leading-6">
							Select payment method to make booking
						</Card.Description>
					</div>
				</div>
			</Card.Header>
			<Card.Content>
				<div class="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
					<div class="rounded-2xl border p-4">
						<div
							class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
						>
							<CalendarDaysIcon class="size-4" />
							Date
						</div>
						<p class="mt-3 text-sm font-semibold">Date</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div
							class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
						>
							<Clock3Icon class="size-4" />
							Time
						</div>
						<p class="mt-3 text-sm font-semibold">Slot Start Time</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div
							class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
						>
							<UserRoundIcon class="size-4" />
							Players
						</div>
						<p class="mt-3 text-sm font-semibold">1 total</p>
					</div>
					<div class="rounded-2xl border p-4">
						<div
							class="text-muted-foreground flex items-center gap-2 text-xs tracking-[0.18em] uppercase"
						>
							<CreditCardIcon class="size-4" />
							Total
						</div>
						<p class="mt-3 text-sm font-semibold">R 999</p>
					</div>
				</div>

				<div class="text-muted-foreground mt-8 mb-2">User Summary</div>
				<Card.Root class="overflow-hidden p-0">
					<Table.Root>
						<Table.Header>
							<Table.Row>
								<Table.Head>Player</Table.Head>
								<Table.Head>Name</Table.Head>
								<Table.Head>Cell No</Table.Head>
								<Table.Head>Email</Table.Head>
							</Table.Row>
						</Table.Header>
						<Table.Body>
							<Table.Row>
								<Table.Cell class="font-medium">1</Table.Cell>
								<Table.Cell>Name</Table.Cell>
								<Table.Cell>234234</Table.Cell>
								<Table.Cell>eamila@amil.com</Table.Cell>
							</Table.Row>
						</Table.Body>
					</Table.Root>
				</Card.Root>

				<div class="mt-4 mb-4 flex items-center justify-between gap-4">
					<div>
						<h2 class="text-lg font-semibold">Choose payment method</h2>
						<p class="text-muted-foreground text-sm">
							Pick the payment method and proceed to payment
						</p>
					</div>
				</div>
				<RadioGroup.Root value="comfortable">
					<div class="flex items-center space-x-2">
						<RadioGroup.Item value="default" id="r1">
							<div class="text-muted-foreground text-xs">Credit</div>
							<div>Credit Card</div>
							<Badge>Credit Icon</Badge>
						</RadioGroup.Item>
						<Label for="r1">Default</Label>
					</div>
					<div class="flex items-center space-x-2">
						<RadioGroup.Item value="comfortable" id="r2" />
						<Label for="r2">Comfortable</Label>
					</div>
					<div class="flex items-center space-x-2">
						<RadioGroup.Item value="compact" id="r3" />
						<Label for="r3">Compact</Label>
					</div>
				</RadioGroup.Root>
				<ToggleGroup.Root type="single" class="w-full border">
					<ToggleGroup.Item value="credit" class="flex h-fit flex-1 flex-col p-2">
						<div class="text-muted-foreground text-xs">Credit</div>
						<div>Credit Card</div>
						<Badge>Credit Icon</Badge>
					</ToggleGroup.Item>
					<ToggleGroup.Item value="cash" class="flex h-fit flex-1 flex-col p-2">
						<div class="text-muted-foreground text-xs">Cash</div>
						<div>Pay cash</div>
						<Badge>Cash Icon</Badge>
					</ToggleGroup.Item>
				</ToggleGroup.Root>
			</Card.Content>
			<Card.Footer class=" flex justify-between border-t">
				<Button href={resolve(`/outlet/${slug}/book/${facilityId}`)} variant="destructive">
					<ChevronLeftIcon class="size-4" />
					Cancel
				</Button>
				<Button>Proceed</Button>
			</Card.Footer>
		</Card.Root>
	</div>
</div>

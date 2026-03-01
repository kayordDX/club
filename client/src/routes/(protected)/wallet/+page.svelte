<script lang="ts">
	import { Badge, Button, Card } from "@kayord/ui";
	import PageHeading from "../settings/PageHeading.svelte";
	import {
		WalletIcon,
		BanknoteArrowUpIcon,
		BanknoteArrowDownIcon,
		WalletMinimalIcon,
	} from "@lucide/svelte";
</script>

<div class="m-4">
	<PageHeading
		title="Wallet"
		description="Secure payments with OTP authentication"
		icon={WalletIcon}
	/>
	<Card.Root class="mt-6">
		<Card.Header class="flex items-center gap-2">
			<div class="bg-background text-foreground rounded-md p-2"><WalletIcon /></div>
			<div>
				<Card.Title>My Wallet</Card.Title>
				<Card.Description>Manage your balance</Card.Description>
			</div>
		</Card.Header>
		<Card.Content>
			<div class="text-muted-foreground text-xs">Current balance</div>
			<div class="text-2xl font-bold">R100.00</div>
		</Card.Content>
		<Card.Footer>
			<Button><BanknoteArrowUpIcon /> Load Money</Button>
		</Card.Footer>
	</Card.Root>

	<Card.Root class="mt-6">
		<Card.Header class="flex items-center gap-2">
			<div class="bg-background text-foreground rounded-md p-2"><WalletMinimalIcon /></div>
			<div>
				<Card.Title>Transaction History</Card.Title>
				<Card.Description>Your recent wallet transactions</Card.Description>
			</div>
		</Card.Header>
		<Card.Content class="flex flex-col gap-4">
			<Card.Root class="flex w-full flex-row items-center gap-4 rounded-md border p-4">
				<div class="w-fit rounded-md bg-green-300 p-2 text-shadow-green-950">
					<BanknoteArrowUpIcon class="text-green-950" />
				</div>
				<div class="flex-1">
					<div class="text-2xl font-bold">Loaded R100</div>
					<div class="text-muted-foreground text-xs">01/03/2026 16:56</div>
				</div>
				<div>
					<Badge class="bg-green-300 text-green-950">+ R100</Badge>
				</div>
			</Card.Root>
			<Card.Root class="flex w-full flex-row items-center gap-4 rounded-md border p-4">
				<div class="bg-destructive dark:bg-destructive/70 w-fit rounded-md p-2 text-white">
					<BanknoteArrowDownIcon />
				</div>
				<div class="flex-1">
					<div class="text-2xl font-bold">Loaded R100</div>
					<div class="text-muted-foreground text-xs">01/03/2026 16:56</div>
				</div>
				<div>
					<Badge variant="destructive">+ R100</Badge>
				</div>
			</Card.Root>
		</Card.Content>
	</Card.Root>
</div>

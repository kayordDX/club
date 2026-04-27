<script>
	import { goto } from "$app/navigation";
	import { networkInformation } from "$lib/stores/network.svelte";
	import { getInitials } from "$lib/util";
	import { Avatar, DropdownMenu } from "@kayord/ui";
	import { LogOutIcon, WalletIcon, SettingsIcon, BookIcon } from "@lucide/svelte";
	import { auth } from "$lib/stores/auth.svelte";
	import { LoginButton } from "../LoginButton";
	import { resolve } from "$app/paths";
</script>

{#if auth.isAuthenticated}
	<DropdownMenu.Root>
		<DropdownMenu.Trigger>
			<div class="relative">
				<Avatar.Root>
					<Avatar.Image src={auth.user?.profile.picture} alt="profile" />
					<Avatar.Fallback class="bg-primary text-primary-foreground">
						{getInitials(`${auth.user?.profile.name}`)}
					</Avatar.Fallback>
				</Avatar.Root>
				<div
					class={`absolute top-0 right-0 size-3 rounded-md ${networkInformation.isOnline() ? "bg-success" : networkInformation.isOnline() ? "bg-destructive animate-pulse" : "bg-muted-foreground"}`}
				></div>
			</div>
		</DropdownMenu.Trigger>
		<DropdownMenu.Content>
			<DropdownMenu.Label>{auth.user?.profile.name}</DropdownMenu.Label>
			<DropdownMenu.Separator />
			<DropdownMenu.Group>
				<DropdownMenu.Item onclick={() => goto(resolve("/bookings"))}>
					<BookIcon />Bookings
				</DropdownMenu.Item>
				<DropdownMenu.Item onclick={() => goto(resolve("/settings/profile"))}>
					<SettingsIcon />Settings
				</DropdownMenu.Item>
				<DropdownMenu.Item onclick={() => goto(resolve("/wallet"))}>
					<WalletIcon />Wallet
				</DropdownMenu.Item>
			</DropdownMenu.Group>
			<DropdownMenu.Separator />
			<DropdownMenu.Item onclick={auth.logout}>
				<LogOutIcon />Log out
			</DropdownMenu.Item>
		</DropdownMenu.Content>
	</DropdownMenu.Root>
{:else}
	<LoginButton />
{/if}

<script lang="ts">
	import { goto } from "$app/navigation";
	import { networkInformation } from "$lib/stores/network.svelte";
	import { getInitials } from "$lib/util";
	import { Avatar, DropdownMenu } from "@kayord/ui";
	import { LogOutIcon, WalletIcon, SettingsIcon, BookIcon } from "@lucide/svelte";
	import { useUser } from "$lib/auth";
	import { LoginButton } from "../LoginButton";
	import { resolve } from "$app/paths";

	const user = useUser();

	const logout = () => {
		// Full navigation so the server clears the session cookie + ends SSO.
		window.location.href = "/auth/logout";
	};
</script>

{#if user}
	<DropdownMenu.Root>
		<DropdownMenu.Trigger>
			<div class="relative">
				<Avatar.Root>
					<Avatar.Image src={user.picture} alt="profile" />
					<Avatar.Fallback class="bg-primary text-primary-foreground">
						{getInitials(user.name)}
					</Avatar.Fallback>
				</Avatar.Root>
				<div class={`absolute top-0 right-0 size-3 rounded-md ${networkInformation.isOnline() ? "bg-success" : "bg-destructive animate-pulse"}`}></div>
			</div>
		</DropdownMenu.Trigger>
		<DropdownMenu.Content>
			<DropdownMenu.Label>{user.name}</DropdownMenu.Label>
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
			<DropdownMenu.Item onclick={logout}>
				<LogOutIcon />Log out
			</DropdownMenu.Item>
		</DropdownMenu.Content>
	</DropdownMenu.Root>
{:else}
	<LoginButton />
{/if}

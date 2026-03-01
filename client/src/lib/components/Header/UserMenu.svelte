<script>
	import { goto } from "$app/navigation";
	import { networkInformation } from "$lib/stores/network.svelte";
	import { getInitials } from "$lib/util";
	import { Avatar, DropdownMenu } from "@kayord/ui";
	import { createLogout } from "$lib/api";
	import { LogOutIcon, WalletIcon, SettingsIcon } from "@lucide/svelte";
	import { user } from "$lib/stores/user.svelte";
	import { LoginButton } from "../LoginButton";
	import { resolve } from "$app/paths";

	const logoutMut = createLogout();
	const logout = async () => {
		await logoutMut.mutateAsync();
		user.clear();
		goto(resolve("/"));
	};
</script>

{#if user.value}
	<DropdownMenu.Root>
		<DropdownMenu.Trigger>
			<div class="relative">
				<Avatar.Root>
					<Avatar.Image src={user.value.picture} alt="profile" />
					<Avatar.Fallback class="bg-primary text-primary-foreground">
						{getInitials(`${user.value.name}`)}
					</Avatar.Fallback>
				</Avatar.Root>
				<div
					class={`absolute top-0 right-0 size-3 rounded-md ${networkInformation.isOnline() ? "bg-success" : networkInformation.isOnline() ? "bg-destructive animate-pulse" : "bg-muted-foreground"}`}
				></div>
			</div>
		</DropdownMenu.Trigger>
		<DropdownMenu.Content>
			<DropdownMenu.Label>{user.value.name}</DropdownMenu.Label>
			<DropdownMenu.Separator />
			<DropdownMenu.Group>
				<DropdownMenu.Item onclick={() => goto(resolve("/settings/session"))}>
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

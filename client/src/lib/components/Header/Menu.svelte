<script>
	import { goto } from "$app/navigation";
	import { networkInformation } from "$lib/stores/network.svelte";
	import { user } from "$lib/stores/user.svelte";
	import { getInitials } from "$lib/util";
	import { Avatar, DropdownMenu } from "@kayord/ui";
	import { createLogout } from "$lib/api";
	import {
		LogOutIcon,
		WrenchIcon,
		NetworkIcon,
		ArrowRightLeft,
		ShieldUserIcon,
		TvMinimalIcon,
	} from "@lucide/svelte";

	const logoutMutation = createLogout();
	const logout = async () => {
		await $logoutMutation.mutateAsync();
		goto("/test");
	};
</script>

<DropdownMenu.Root>
	<DropdownMenu.Trigger>
		<div class="relative">
			<Avatar.Root>
				<Avatar.Image src={user.value?.id} alt="profile" />
				<Avatar.Fallback class="bg-primary text-primary-foreground">
					{getInitials(`${user.value?.firstName} ${user.value?.lastName}`)}
				</Avatar.Fallback>
			</Avatar.Root>
			<div
				class={`absolute right-0 top-0 size-3 rounded-md ${networkInformation.isOnline() ? "bg-success" : networkInformation.isOnline() ? "bg-destructive animate-pulse" : "bg-muted-foreground"}`}
			></div>
		</div>
	</DropdownMenu.Trigger>
	<DropdownMenu.Content>
		<DropdownMenu.Label>{user.value?.firstName ?? "My Account"}</DropdownMenu.Label>
		<DropdownMenu.Separator />
		<DropdownMenu.Group>
			<DropdownMenu.Item onclick={() => goto("/switch")}>
				<ArrowRightLeft class="mr-2 h-4 w-4" />Switch Outlet
			</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => goto("/setup")}>
				<WrenchIcon class="mr-2 h-4 w-4" />Setup Device
			</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => goto("/link-account")}>
				<ShieldUserIcon class="mr-2 h-4 w-4" />Link Account
			</DropdownMenu.Item>
			<DropdownMenu.Item onclick={() => goto("/network")}>
				<NetworkIcon class="mr-2 h-4 w-4" />Network Information
			</DropdownMenu.Item>
		</DropdownMenu.Group>
		<DropdownMenu.Separator />
		<DropdownMenu.Item onclick={logout}>
			<LogOutIcon class="mr-2 h-4 w-4" />
			<span>Log out</span>
		</DropdownMenu.Item>
	</DropdownMenu.Content>
</DropdownMenu.Root>

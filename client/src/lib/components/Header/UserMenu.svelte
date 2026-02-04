<script>
	import { goto } from "$app/navigation";
	import { networkInformation } from "$lib/stores/network.svelte";
	import { getInitials } from "$lib/util";
	import { Avatar, DropdownMenu } from "@kayord/ui";
	import { createLogout } from "$lib/api";
	import { LogOutIcon, WrenchIcon, ArrowRightLeft } from "@lucide/svelte";
	import { user } from "$lib/stores/user.svelte";
	import { LoginButton } from "../LoginButton";

	const logoutMut = createLogout();
	const logout = async () => {
		await logoutMut.mutateAsync();
		user.clear();
		goto("/");
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
				<DropdownMenu.Item onclick={() => goto("/login")}>
					<ArrowRightLeft class="mr-2 h-4 w-4" />Login
				</DropdownMenu.Item>
				<DropdownMenu.Item onclick={() => goto("/another")}>
					<WrenchIcon class="mr-2 h-4 w-4" />Another
				</DropdownMenu.Item>
			</DropdownMenu.Group>
			<DropdownMenu.Separator />
			<DropdownMenu.Item onclick={logout}>
				<LogOutIcon class="mr-2 h-4 w-4" />
				<span>Log out</span>
			</DropdownMenu.Item>
		</DropdownMenu.Content>
	</DropdownMenu.Root>
{:else}
	<LoginButton />
{/if}

<script lang="ts">
	import { resolve } from "$app/paths";
	import { page } from "$app/state";
	import { Sidebar } from "@kayord/ui";
	import { ShieldIcon, UserIcon } from "@lucide/svelte";

	const menuItems = [
		{
			title: "Session",
			url: "/settings/session",
			icon: ShieldIcon,
			isActive: true,
		},
		{
			title: "Profile",
			url: "/settings/profile",
			icon: UserIcon,
		},
	];

	const activeItem = $derived.by(() => {
		const routeId = page.route.id?.replaceAll("/[Id]", "");
		for (const item of menuItems) {
			if (item.url.length > 0 && routeId?.endsWith(item.url)) {
				return item;
			}
		}
		return;
	});
</script>

<Sidebar.Group>
	<Sidebar.GroupLabel>Settings</Sidebar.GroupLabel>
	<Sidebar.Menu>
		{#each menuItems as item (item.title)}
			<Sidebar.MenuItem>
				<Sidebar.MenuButton
					isActive={activeItem?.title === item.title}
					class="text-muted-foreground"
				>
					{#snippet child({ props })}
						<a href={resolve(item.url)} {...props}>
							<item.icon />
							<span>{item.title}</span>
						</a>
					{/snippet}
				</Sidebar.MenuButton>
			</Sidebar.MenuItem>
		{/each}
	</Sidebar.Menu>
</Sidebar.Group>

<script lang="ts">
	import Header from "$lib/components/Header/Header.svelte";
	import LogoButton from "$lib/components/LogoButton.svelte";
	import { Sidebar } from "@kayord/ui";
	import SettingsMenu from "./SettingsMenu.svelte";
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";

	let { children } = $props();
</script>

{#snippet sidebarTrigger()}
	<Sidebar.Trigger />
{/snippet}

<Sidebar.Provider>
	<Sidebar.Root>
		<Sidebar.Header class="p-0">
			<div
				class="border-border bg-card/50 flex h-14 items-center justify-between border-b p-2 backdrop-blur-sm"
			>
				<LogoButton />
			</div>
		</Sidebar.Header>
		<Sidebar.Content>
			<SettingsMenu />
		</Sidebar.Content>
		<Sidebar.Rail />
	</Sidebar.Root>
	<Sidebar.Inset>
		<AuthCheck>
			<Header leftHeader={sidebarTrigger} />
			{@render children?.()}
		</AuthCheck>
	</Sidebar.Inset>
</Sidebar.Provider>

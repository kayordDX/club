<script lang="ts">
	import RoleCheck from "$lib/components/check/RoleCheck.svelte";
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";
	import { Sidebar } from "@kayord/ui";
	import AdminSidebar from "./AdminSidebar.svelte";
	import Header from "$lib/components/Header/Header.svelte";
	import { setRolesContext } from "$lib/auth";

	let { data, children } = $props();
	// The @ reset detaches this layout from the outlet layout, so re-publish
	// roles into context for RoleCheck.
	// svelte-ignore state_referenced_locally
	setRolesContext(data.roles);
</script>

{#snippet test()}
	<Sidebar.Trigger />
{/snippet}

<AuthCheck>
	<RoleCheck roles={["MANAGER"]}>
		<Sidebar.Provider>
			<AdminSidebar />
			<main class="w-full">
				<Header leftHeader={test} />
				{@render children?.()}
			</main>
		</Sidebar.Provider>
	</RoleCheck>
</AuthCheck>

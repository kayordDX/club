<script lang="ts">
	import RoleCheck from "$lib/components/check/RoleCheck.svelte";
	import { Sidebar } from "@kayord/ui";
	import AdminSidebar from "./AdminSidebar.svelte";
	import AuthCheck from "$lib/components/check/AuthCheck.svelte";
	import Header from "$lib/components/Header/Header.svelte";
	import { onMount } from "svelte";
	import { auth } from "$lib/stores/auth.svelte";
	import { page } from "$app/state";

	let { children } = $props();

	onMount(async () => {
		if (auth.isAuthenticated) {
			await auth.getRoles(Number(page.params.id));
		}
	});
</script>

{#snippet test()}
	<Sidebar.Trigger />
{/snippet}

<AuthCheck isProtected={true}>
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

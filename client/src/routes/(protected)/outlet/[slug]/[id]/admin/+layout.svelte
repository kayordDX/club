<script lang="ts">
	import RoleCheck from "$lib/components/check/RoleCheck.svelte";
	import AdminNav from "./AdminNav.svelte";

	let { children } = $props();
</script>

<RoleCheck roles={["MANAGER"]}>
	<div class="flex flex-col gap-4 md:flex-row md:items-start">
		<AdminNav />
		<div class="min-w-0 flex-1">
			{@render children?.()}
		</div>
	</div>
</RoleCheck>

<script lang="ts">
	import "../layout.css";
	import favicon from "$lib/assets/favicon.svg";
	import { ModeWatcher } from "mode-watcher";
	import { Toaster, toast } from "svelte-sonner";
	import { setUserContext } from "$lib/auth";
	import PageBoundary from "$lib/components/PageBoundary.svelte";

	let { children, data } = $props();

	// svelte-ignore state_referenced_locally
	setUserContext(data.user);

	const handleToasterClick = (event: MouseEvent) => {
		if ((event.target as Element).closest("button")) return;
		const toastEl = (event.target as Element).closest("[data-sonner-toast]");
		if (!toastEl) return;
		const target = toast.getActiveToasts()[Number(toastEl.getAttribute("data-index"))];
		if (target) toast.dismiss(target.id);
	};
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

<ModeWatcher defaultMode="dark" />
<Toaster onclick={handleToasterClick} />

<PageBoundary>
	{@render children?.()}
</PageBoundary>

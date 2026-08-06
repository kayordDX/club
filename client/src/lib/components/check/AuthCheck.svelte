<script lang="ts">
	import type { Snippet } from "svelte";

	// Protection is now enforced server-side in +layout.server.ts. This component
	// is kept as a pass-through so existing templates don't need to change.
	interface Props {
		children?: Snippet;
	}

	let { children }: Props = $props();
</script>

{@render children?.()}

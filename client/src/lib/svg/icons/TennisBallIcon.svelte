<script lang="ts">
	import { Icon, type IconNode } from "@lucide/svelte";

	let { ...rest } = $props();

	const tennisBallIcon: IconNode = [
		[
			"path",
			{
				d: "M15.5 3.206A9 9 0 1 1 2 11C2 4.072 9.5-.258 15.5 3.206",
			},
		],
		[
			"path",
			{
				d: "M17 4.3c-3.988 3.577-3.988 9.823 0 13.4",
			},
		],
		[
			"path",
			{
				d: "M5 4.3c3.988 3.577 3.988 9.823 0 13.4",
			},
		],
	];
</script>

<Icon iconNode={tennisBallIcon} {...rest} />

<script lang="ts">
	import { Icon, type IconNode } from "@lucide/svelte";

	let { ...rest } = $props();

	const golfIcon: IconNode = [
		[
			"path",
			{
				d: "M12 18V3l7 4-7 4",
			},
		],
		[
			"path",
			{
				d: "M9 17.67c-.62.36-1 .82-1 1.33 0 1.1 1.8 2 4 2s4-.9 4-2c0-.5-.38-.97-1-1.33",
			},
		],
	];
</script>

<Icon iconNode={golfIcon} {...rest} />

<script lang="ts">
	import { goto } from "$app/navigation";
	import { resolve } from "$app/paths";
</script>

<button class="flex items-center gap-1" onclick={() => goto(resolve("/"))}>
	<img src="/favicon.svg" alt="logo" class="h-10" />
	<span class="hidden text-lg font-bold md:block">Online</span>
</button>

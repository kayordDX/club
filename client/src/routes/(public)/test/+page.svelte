<script lang="ts">
	import { Markdown } from "#lib/components/Markdown/index.js";
	import { Textarea } from "@kayord/ui";

	let source = $state(`yrdy`);
</script>

<div class="mx-auto flex max-w-4xl flex-col gap-6 p-6">
	<Textarea bind:value={source} class="min-h-64 font-mono" />

	<Markdown {source} />
</div>

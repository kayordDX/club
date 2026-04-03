<script lang="ts">
	import type { AnyFormApi } from "@tanstack/svelte-form";
	import type { Snippet } from "svelte";

	type Props = {
		form: AnyFormApi;
		children?: Snippet;
	};

	let { form, children }: Props = $props();
</script>

<form
	id={form.formId}
	onsubmit={(e) => {
		e.preventDefault();
		e.stopPropagation();
		form.handleSubmit();
	}}
>
	{@render children?.()}
</form>

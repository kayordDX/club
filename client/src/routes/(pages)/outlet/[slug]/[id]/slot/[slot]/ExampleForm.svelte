<script lang="ts">
	import { type FormApi } from "./schema";
	type Props = {
		form: FormApi;
	};

	let { form }: Props = $props();
</script>

<div>
	{JSON.stringify(form)}
</div>

<form.AppField name="players">
	{#snippet children(field)}
		{field.state.value[0].name}
	{/snippet}
</form.AppField>

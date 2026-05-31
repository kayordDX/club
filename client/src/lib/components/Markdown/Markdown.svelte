<script lang="ts">
	import SvelteMarkdown, { type SvelteMarkdownProps } from "@humanspeak/svelte-markdown";
	import { Checkbox } from "@kayord/ui";
	import { cn } from "@kayord/ui/utils";

	interface Props extends SvelteMarkdownProps {
		class?: string;
	}

	let props: Props = $props();
</script>

<article
	class={[
		"prose prose-muted max-w-none",
		"prose-headings:text-card-foreground prose-p:text-card-foreground prose-strong:text-card-foreground",
		"prose-a:text-primary prose-a:underline prose-a:underline-offset-4",
		"prose-code:rounded prose-code:bg-muted prose-code:px-1.5 prose-code:py-0.5 prose-code:text-foreground",
		"prose-pre:bg-muted prose-pre:text-foreground",
		"prose-blockquote:border-border prose-blockquote:text-muted-foreground",
		"prose-th:text-card-foreground prose-td:text-card-foreground prose-th:font-bold prose-th:p-2 prose-td:p-2",
		"prose-li:text-foreground",
		cn(props.class),
	]}
>
	<SvelteMarkdown {...props}>
		{#snippet table({ children })}
			<div class="overflow-x-auto rounded-md border">
				<table class="my-0! w-full">{@render children?.()}</table>
			</div>
		{/snippet}

		{#snippet listitem({ task, checked, children })}
			{#if task}
				<li class="list-none">
					<div class="flex items-center gap-2">
						<Checkbox {checked} disabled />
						<span class:done={checked}>{@render children?.()}</span>
					</div>
				</li>
			{:else}
				<li>
					{@render children?.()}
				</li>
			{/if}
		{/snippet}
	</SvelteMarkdown>
</article>

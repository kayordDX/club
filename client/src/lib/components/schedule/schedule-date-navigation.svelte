<script lang="ts">
	import { Button, ButtonGroup, Popover } from "@kayord/ui";
	import { Calendar } from "@kayord/ui/calendar";
	import { CalendarIcon, ChevronLeftIcon, ChevronRightIcon } from "@lucide/svelte";
	import { DateFormatter, getLocalTimeZone, type CalendarDate, type DateValue } from "@internationalized/date";

	type Props = {
		value: CalendarDate;
		onChange: (..._args: [string]) => void;
	};

	let { value, onChange }: Props = $props();
	let datePickerOpen = $state(false);
	const formatter = new DateFormatter("en-ZA", { dateStyle: "long" });
</script>

<div class="flex items-center gap-2">
	<div class="hidden flex-col sm:flex">
		<div class="font-bold">Select Date</div>
		<div class="text-muted-foreground text-xs">Pick a day</div>
	</div>
	<ButtonGroup.Root>
		<Button size="icon" variant="outline" aria-label="Previous day" onclick={() => onChange(value.add({ days: -1 }).toString())}>
			<ChevronLeftIcon />
		</Button>
		<Popover.Root bind:open={datePickerOpen}>
			<Popover.Trigger>
				{#snippet child({ props })}
					<Button variant="outline" class="w-70 justify-start text-start font-normal" aria-label="Select date" {...props}>
						<CalendarIcon class="me-2 size-4" />
						{formatter.format(value.toDate(getLocalTimeZone()))}
					</Button>
				{/snippet}
			</Popover.Trigger>
			<Popover.Content class="w-auto p-0">
				<Calendar
					{value}
					onValueChange={(next: DateValue | undefined) => {
						if (!next) return;
						onChange(next.toString());
						datePickerOpen = false;
					}}
					type="single"
					initialFocus
					captionLayout="dropdown"
				/>
			</Popover.Content>
		</Popover.Root>
		<Button size="icon" variant="outline" aria-label="Next day" onclick={() => onChange(value.add({ days: 1 }).toString())}>
			<ChevronRightIcon />
		</Button>
	</ButtonGroup.Root>
</div>

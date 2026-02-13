<script>
	import { Card, ToggleGroup } from "@kayord/ui";
</script>

<Card.Root>
	<Card.Header>
		<Card.Title>Select Facility</Card.Title>
		<Card.Description>Select the facility</Card.Description>
	</Card.Header>
	<Card.Content>
		<ToggleGroup.Root size="sm" type="single" variant="outline">
			<ToggleGroup.Item value="bold" aria-label="Toggle bold">Item1</ToggleGroup.Item>
			<ToggleGroup.Item value="italic" aria-label="Toggle italic">Item2</ToggleGroup.Item>
			<ToggleGroup.Item value="strikethrough" aria-label="Toggle strikethrough"
				>Item3</ToggleGroup.Item
			>
		</ToggleGroup.Root>
	</Card.Content>
</Card.Root>

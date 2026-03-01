<script lang="ts">
	import { UserIcon } from "@lucide/svelte";
	import PageHeading from "../PageHeading.svelte";
	import { Avatar, Card, Table } from "@kayord/ui";
	import { user } from "$lib/stores/user.svelte";
	import { getInitials } from "$lib/util";
</script>

<div class="m-4">
	<PageHeading title="Profile" description="Manage your user profile." icon={UserIcon} />

	<Card.Root class="mt-6">
		<Card.Header class="flex items-center gap-2">
			<Avatar.Root>
				<Avatar.Image src={user.value?.picture} alt="profile" />
				<Avatar.Fallback class="bg-primary text-primary-foreground">
					{getInitials(`${user.value?.name}`)}
				</Avatar.Fallback>
			</Avatar.Root>
			<div class="flex flex-col justify-center">
				<Card.Title>Profile Information</Card.Title>
				<Card.Description>Current information of logged in user</Card.Description>
			</div>
		</Card.Header>
		<Card.Content>
			<Table.Root>
				<Table.Body>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Name</Table.Cell>
						<Table.Cell class="text-end">{user.value?.name}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">First Name</Table.Cell>
						<Table.Cell class="text-end">{user.value?.firstName}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Last Name</Table.Cell>
						<Table.Cell class="text-end">{user.value?.lastName}</Table.Cell>
					</Table.Row>
					<Table.Row>
						<Table.Cell class="text-muted-foreground text-sm">Email</Table.Cell>
						<Table.Cell class="text-end">{user.value?.email}</Table.Cell>
					</Table.Row>
				</Table.Body>
			</Table.Root>
		</Card.Content>
	</Card.Root>
</div>
